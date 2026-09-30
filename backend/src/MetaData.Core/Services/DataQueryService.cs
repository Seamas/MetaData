using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MetaData.Abstractions;
using MetaData.Core.Data;
using MetaData.Providers.Dialects;
using MetaData.Core.Entities;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Exceptions;
using MetaData.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Core.Services;

/// <summary>基于 ADO.NET 的元数据驱动单表查询（别名映射、参数化条件、方言分页）。</summary>
public class DataQueryService
{
    private const int MaxPageSize = 500;

    private static readonly Regex DateOnlyPattern = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);

    private readonly IDbContextFactory<MetaDataDbContext> _contextFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDialectRegistry _dialects;
    private readonly ICurrentUser _currentUser;

    public DataQueryService(
        IDbContextFactory<MetaDataDbContext> contextFactory,
        IDbConnectionFactory connectionFactory,
        IDialectRegistry dialects,
        ICurrentUser currentUser)
    {
        _contextFactory = contextFactory;
        _connectionFactory = connectionFactory;
        _dialects = dialects;
        _currentUser = currentUser;
    }

    public async Task<List<PublishedTableDto>> GetPublishedTablesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Tables.AsNoTracking()
            .Where(t => t.IsPublished && t.Connection!.IsEnabled)
            .OrderBy(t => t.Connection!.Name).ThenBy(t => t.DisplayName).ThenBy(t => t.TableName)
            .Select(t => new PublishedTableDto
            {
                TableId = t.Id,
                ConnectionId = t.ConnectionId,
                ConnectionName = t.Connection!.Name,
                Schema = t.Schema,
                TableName = t.TableName,
                DisplayName = t.DisplayName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DataQueryResponse> QueryAsync(DataQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TableId <= 0)
        {
            throw new MetaDataException("请选择要查询的表。");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? 20 : request.PageSize;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var table = await context.Tables.AsNoTracking()
            .Include(t => t.Connection)
            .FirstOrDefaultAsync(t => t.Id == request.TableId, cancellationToken)
            ?? throw new MetaDataException($"表不存在：{request.TableId}");

        if (!table.IsPublished)
        {
            throw new MetaDataException("该表尚未开放查询。");
        }

        if (table.Connection is null || !table.Connection.IsEnabled)
        {
            throw new MetaDataException("该表所属连接已停用。");
        }

        var fields = await context.Fields.AsNoTracking()
            .Where(f => f.TableId == table.Id)
            .OrderBy(f => f.Ordinal)
            .ToListAsync(cancellationToken);
        if (fields.Count == 0)
        {
            throw new MetaDataException("该表没有可用字段元数据。");
        }

        var fieldByAlias = fields.ToDictionary(f => f.Alias, StringComparer.OrdinalIgnoreCase);
        var fieldByName = fields.ToDictionary(f => f.FieldName, StringComparer.OrdinalIgnoreCase);

        // 用户偏好合并：覆盖顺序与显隐
        var preferences = (await context.UserFieldPreferences.AsNoTracking()
                .Where(p => p.UserId == _currentUser.UserId && p.TableId == table.Id)
                .ToListAsync(cancellationToken))
            .ToDictionary(p => p.FieldId);

        var effectiveColumns = fields
            .Select(f =>
            {
                preferences.TryGetValue(f.Id, out var pref);
                return (Field: f,
                        Ordinal: pref is null ? f.Ordinal : pref.Ordinal,
                        Visible: pref is null ? f.IsVisible : pref.IsVisible,
                        Width: pref?.Width);
            })
            .OrderBy(x => x.Ordinal)
            .ToList();

        var visibleColumns = effectiveColumns.Where(x => x.Visible).Select(x => x.Field).ToList();
        if (visibleColumns.Count == 0)
        {
            throw new MetaDataException("没有可显示的列，请在列设置中至少显示一列。");
        }

        // 过滤条件：别名 → 物理字段
        var filters = new List<FilterCondition>();
        foreach (var dto in request.Filters.Where(f => f is not null))
        {
            if (!fieldByAlias.TryGetValue(dto.Alias, out var field))
            {
                throw new MetaDataException($"未知字段别名：{dto.Alias}");
            }

            if (!QueryOperatorMap.IsOperatorAllowed(field.DataCategory, dto.Operator))
            {
                throw new MetaDataException($"字段“{field.DisplayName ?? field.FieldName}”不支持操作符：{dto.Operator}");
            }

            var info = QueryOperatorMap.OperatorInfo[dto.Operator];
            object? value = null;
            object? value2 = null;
            if (info.NeedsValue)
            {
                value = ConvertFilterValue(field, dto.Value);
                if (value is null)
                {
                    throw new MetaDataException($"字段“{field.DisplayName ?? field.FieldName}”的查询值不能为空。");
                }
            }

            if (info.NeedsValue2)
            {
                value2 = ConvertFilterValue(field, dto.Value2);
                if (value2 is null)
                {
                    throw new MetaDataException($"字段“{field.DisplayName ?? field.FieldName}”的区间结束值不能为空。");
                }
            }

            filters.Add(new FilterCondition
            {
                ColumnName = field.FieldName,
                Operator = dto.Operator,
                Value = value,
                Value2 = value2
            });
        }

        // 排序：别名 → 物理字段；无排序时回退 默认排序字段/主键/首列
        var sorts = new List<SortItem>();
        foreach (var dto in request.Sorts.Where(s => !string.IsNullOrWhiteSpace(s.Alias)))
        {
            if (!fieldByAlias.TryGetValue(dto.Alias, out var field))
            {
                throw new MetaDataException($"未知字段别名：{dto.Alias}");
            }

            sorts.Add(new SortItem { ColumnName = field.FieldName, Direction = dto.Direction });
        }

        if (sorts.Count == 0)
        {
            string? fallback = null;
            if (!string.IsNullOrWhiteSpace(table.DefaultSortField) &&
                fieldByName.ContainsKey(table.DefaultSortField!))
            {
                fallback = table.DefaultSortField;
            }

            fallback ??= fields.FirstOrDefault(f => f.IsPrimaryKey)?.FieldName
                        ?? visibleColumns[0].FieldName;

            sorts.Add(new SortItem { ColumnName = fallback, Direction = SortDirection.Asc });
        }

        var dialect = _dialects.Resolve(table.Connection.DatabaseType);
        var version = string.IsNullOrWhiteSpace(table.Connection.ServerVersion)
            ? DbVersionInfo.Unknown
            : dialect.ParseVersion(table.Connection.ServerVersion);

        var parts = new QueryParts
        {
            Schema = table.Schema,
            TableName = table.TableName,
            SelectColumns = visibleColumns.Select(f => f.FieldName).ToList(),
            Filters = filters,
            Sorts = sorts,
            Skip = (long)(page - 1) * pageSize,
            Take = pageSize
        };

        var countSql = dialect.BuildCountQuery(parts);
        var pageSql = dialect.BuildPagedQuery(parts, version);

        var rows = new List<Dictionary<string, object?>>();
        long total;

        await using (var connection = await _connectionFactory.OpenAsync(table.Connection, cancellationToken))
        {
            await using (var countCommand = CreateCommand(connection, countSql, dialect.ParameterPrefix))
            {
                var result = await countCommand.ExecuteScalarAsync(cancellationToken);
                total = Convert.ToInt64(result ?? 0L);
            }

            await using var pageCommand = CreateCommand(connection, pageSql, dialect.ParameterPrefix);
            await using var reader = await pageCommand.ExecuteReaderAsync(cancellationToken);

            var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in visibleColumns)
            {
                ordinals[f.Alias] = reader.GetOrdinal(f.FieldName);
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var f in visibleColumns)
                {
                    var ordinal = ordinals[f.Alias];
                    var raw = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
                    row[f.Alias] = NormalizeValue(raw);
                }

                rows.Add(row);
            }
        }

        return new DataQueryResponse
        {
            Columns = visibleColumns.Select(f => new ColumnDto
            {
                Alias = f.Alias,
                DisplayName = string.IsNullOrWhiteSpace(f.DisplayName) ? f.FieldName : f.DisplayName,
                DataCategory = f.DataCategory,
                NativeDataType = f.NativeDataType,
                Width = preferences.TryGetValue(f.Id, out var p) ? p.Width : null,
                IsPrimaryKey = f.IsPrimaryKey
            }).ToList(),
            Rows = rows,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>将前端 JSON 值按字段类型转换为绑定 CLR 值。</summary>
    private static object? ConvertFilterValue(FieldMetadata field, JsonElement? element)
    {
        if (element is null || element.Value.ValueKind == JsonValueKind.Null ||
            element.Value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var v = element.Value;
        if (v.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(v.GetString()))
        {
            return null;
        }

        switch (field.DataCategory)
        {
            case DataCategory.Number:
            {
                if (!decimal.TryParse(v.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    throw new MetaDataException($"“{field.DisplayName ?? field.FieldName}”不是有效的数字。");
                }

                // 整数列绑定 long；显式小数列绑定 decimal；浮点列绑定 double
                if (field.NumericScale is 0 && d.Scale == 0)
                {
                    return (long)d;
                }

                if ((field.NumericPrecision.HasValue && field.NumericScale is > 0) || d.Scale > 0)
                {
                    return d;
                }

                return (double)d;
            }

            case DataCategory.DateTime:
            {
                if (v.ValueKind != JsonValueKind.String)
                {
                    throw new MetaDataException($"“{field.DisplayName ?? field.FieldName}”需要日期时间值。");
                }

                var s = v.GetString()!;
                if (DateOnlyPattern.IsMatch(s))
                {
                    return DateTime.SpecifyKind(DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                }

                if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces, out var dt))
                {
                    throw new MetaDataException($"“{field.DisplayName ?? field.FieldName}”日期格式无法识别：{s}");
                }

                return dt;
            }

            case DataCategory.Boolean:
            {
                if (v.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return v.GetBoolean();
                }

                if (bool.TryParse(v.GetRawText(), out var b))
                {
                    return b;
                }

                throw new MetaDataException($"“{field.DisplayName ?? field.FieldName}”需要布尔值。");
            }

            case DataCategory.Guid:
            {
                if (Guid.TryParse(v.GetString(), out var g))
                {
                    return g;
                }

                throw new MetaDataException($"“{field.DisplayName ?? field.FieldName}”不是有效的唯一标识。");
            }

            case DataCategory.Binary:
                throw new MetaDataException($"“{field.DisplayName ?? field.FieldName}”为二进制字段，不支持条件查询。");

            default:
                return v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText();
        }
    }

    private static DbCommand CreateCommand(DbConnection connection, BuiltSql built, char parameterPrefix)
    {
        var command = connection.CreateCommand();
        command.CommandText = built.Sql;

        foreach (var (name, value) in built.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterPrefix + name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        // Oracle ODP.NET 默认按位置绑定，强制按名称绑定，参数添加顺序不受 SQL 中出现顺序约束
        command.GetType().GetProperty("BindByName")?.SetValue(command, true);
        return command;
    }

    private static object? NormalizeValue(object? raw)
    {
        // byte[] 由 System.Text.Json 自动按 Base64 输出；其余类型（long/decimal/DateTime/bool/Guid/string）保持原样
        return raw switch
        {
            DBNull => null,
            DateTimeOffset dto => dto.UtcDateTime,
            _ => raw
        };
    }
}

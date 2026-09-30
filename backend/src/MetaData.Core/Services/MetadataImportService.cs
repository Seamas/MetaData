using MetaData.Abstractions;
using MetaData.Abstractions.Schema;
using MetaData.Core.Data;
using MetaData.Providers.Dialects;
using MetaData.Core.Entities;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Exceptions;
using MetaData.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Core.Services;

/// <summary>从业务库读取结构并合并写入元数据表（已存在字段保留用户自定义配置）。</summary>
public class MetadataImportService
{
    private readonly IDbContextFactory<MetaDataDbContext> _contextFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDialectRegistry _dialects;
    private readonly ISchemaInspectorRegistry _inspectors;

    public MetadataImportService(
        IDbContextFactory<MetaDataDbContext> contextFactory,
        IDbConnectionFactory connectionFactory,
        IDialectRegistry dialects,
        ISchemaInspectorRegistry inspectors)
    {
        _contextFactory = contextFactory;
        _connectionFactory = connectionFactory;
        _dialects = dialects;
        _inspectors = inspectors;
    }

    public async Task<MetadataImportResultDto> ImportAsync(MetadataImportRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Tables.Count == 0)
        {
            throw new MetaDataException("请至少选择一张表。");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var connection = await context.Connections.FirstOrDefaultAsync(x => x.Id == request.ConnectionId, cancellationToken)
                         ?? throw new MetaDataException($"连接不存在：{request.ConnectionId}");

        var dialect = _dialects.Resolve(connection.DatabaseType);
        var inspector = _inspectors.Resolve(connection.DatabaseType);

        var wanted = request.Tables
            .GroupBy(t => (t.Schema ?? string.Empty).Trim() + "|" + t.TableName.Trim())
            .Select(g => g.First())
            .Select(t => inspector.CreateTableSample(t.Schema?.Trim(), t.TableName.Trim()))
            .ToList();

        IReadOnlyList<IColumnSchemaSample> columns;
        await using (var adoConn = await _connectionFactory.OpenAsync(connection, cancellationToken))
        {
            columns = await inspector.GetColumnsAsync(adoConn, connection.DefaultSchema, wanted, cancellationToken);
        }

        var result = new MetadataImportResultDto();
        var now = DateTime.Now;

        foreach (var sampleTable in wanted)
        {
            var tableColumns = columns
                .Where(c => c.TableName == sampleTable.TableName
                            && (string.IsNullOrWhiteSpace(sampleTable.Schema) || c.Schema == sampleTable.Schema))
                .OrderBy(c => c.Ordinal)
                .ToList();

            if (tableColumns.Count == 0)
            {
                // 表不存在或无权限，跳过
                continue;
            }

            var table = await context.Tables.FirstOrDefaultAsync(
                x => x.ConnectionId == connection.Id && x.Schema == sampleTable.Schema && x.TableName == sampleTable.TableName,
                cancellationToken);

            if (table is null)
            {
                table = new TableMetadata
                {
                    ConnectionId = connection.Id,
                    Schema = sampleTable.Schema,
                    TableName = sampleTable.TableName,
                    DisplayName = sampleTable.Comment,
                    IsPublished = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.Tables.Add(table);
                await context.SaveChangesAsync(cancellationToken); // 拿到表 Id
                result.AddedTables++;
            }
            else
            {
                result.UpdatedTables++;
            }

            var existingFields = await context.Fields
                .Where(x => x.TableId == table.Id)
                .ToListAsync(cancellationToken);
            var existingByName = existingFields.ToDictionary(f => f.FieldName);
            var maxOrdinal = existingFields.Count == 0 ? 0 : existingFields.Max(f => f.Ordinal);

            var incomingNames = new HashSet<string>();
            foreach (var col in tableColumns)
            {
                incomingNames.Add(col.ColumnName);
                var category = dialect.MapDataType(col.DataTypeName, col.NativeDataType, col.NumericPrecision, col.NumericScale);

                if (existingByName.TryGetValue(col.ColumnName, out var field))
                {
                    // 保留 Alias/DisplayName/Ordinal/IsVisible，仅刷新物理属性
                    field.NativeDataType = col.NativeDataType ?? col.DataTypeName;
                    field.DataCategory = category;
                    field.MaxLength = col.MaxLength;
                    field.NumericPrecision = col.NumericPrecision;
                    field.NumericScale = col.NumericScale;
                    field.IsNullable = col.IsNullable;
                    field.IsPrimaryKey = col.IsPrimaryKey;
                    field.UpdatedAt = now;
                    result.UpdatedFields++;
                }
                else
                {
                    var alias = EnsureUniqueAlias(col.ColumnName, existingFields);
                    context.Fields.Add(new FieldMetadata
                    {
                        TableId = table.Id,
                        FieldName = col.ColumnName,
                        Alias = alias,
                        DisplayName = col.Comment,
                        Ordinal = ++maxOrdinal,
                        IsVisible = true,
                        NativeDataType = col.NativeDataType ?? col.DataTypeName,
                        DataCategory = category,
                        MaxLength = col.MaxLength,
                        NumericPrecision = col.NumericPrecision,
                        NumericScale = col.NumericScale,
                        IsNullable = col.IsNullable,
                        IsPrimaryKey = col.IsPrimaryKey,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    existingFields.Add(new FieldMetadata { FieldName = col.ColumnName, Alias = alias });
                    result.AddedFields++;
                }
            }

            // 业务库已缺失的字段仅报告，不自动删除
            result.MissingFields.AddRange(existingFields
                .Where(f => !incomingNames.Contains(f.FieldName))
                .Select(f => $"{sampleTable.TableName}.{f.FieldName}"));

            table.UpdatedAt = now;
            await context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static string EnsureUniqueAlias(string alias, IEnumerable<FieldMetadata> existingFields)
    {
        var aliases = new HashSet<string>(existingFields.Select(f => f.Alias), StringComparer.OrdinalIgnoreCase);
        if (!aliases.Contains(alias))
        {
            return alias;
        }

        var suffix = 1;
        string candidate;
        do
        {
            candidate = alias + "_" + suffix++;
        } while (aliases.Contains(candidate));

        return candidate;
    }
}

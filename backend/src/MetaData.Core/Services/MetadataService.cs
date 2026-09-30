using System.Text.RegularExpressions;
using MetaData.Core.Data;
using MetaData.Core.Entities;
using MetaData.Abstractions.Exceptions;
using MetaData.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Core.Services;

/// <summary>表/字段元数据查看与编辑。</summary>
public partial class MetadataService
{
    private static readonly Regex AliasPattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly IMetaDataDbContext _dbContext;

    public MetadataService(IMetaDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TableDto>> GetTablesAsync(long connectionId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tables.AsNoTracking().AsQueryable();
        if (connectionId > 0)
        {
            query = query.Where(x => x.ConnectionId == connectionId);
        }

        var tables = await query.OrderBy(x => x.ConnectionId).ThenBy(x => x.Schema).ThenBy(x => x.TableName).ToListAsync(cancellationToken);
        var fieldCounts = await _dbContext.Fields.AsNoTracking()
            .GroupBy(f => f.TableId)
            .Select(g => new { TableId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TableId, x => x.Count, cancellationToken);

        return tables.Select(t => new TableDto
        {
            Id = t.Id,
            ConnectionId = t.ConnectionId,
            Schema = t.Schema,
            TableName = t.TableName,
            DisplayName = t.DisplayName,
            IsPublished = t.IsPublished,
            DefaultSortField = t.DefaultSortField,
            Remark = t.Remark,
            FieldCount = fieldCounts.GetValueOrDefault(t.Id)
        }).ToList();
    }

    public async Task<List<FieldDto>> GetFieldsAsync(long tableId, CancellationToken cancellationToken = default)
    {
        var fields = await _dbContext.Fields.AsNoTracking()
            .Where(x => x.TableId == tableId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync(cancellationToken);
        return fields.Select(ToDto).ToList();
    }

    public async Task<long> SaveTableAsync(TableDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.TableName))
        {
            throw new MetaDataException("表名不能为空。");
        }


        var exists = await _dbContext.Tables.AsNoTracking().AnyAsync(
            x => x.ConnectionId == dto.ConnectionId && x.Schema == dto.Schema && x.TableName == dto.TableName && x.Id != dto.Id,
            cancellationToken);
        if (exists)
        {
            throw new MetaDataException("该连接下已存在同名表。");
        }

        var entity = dto.Id > 0
            ? await _dbContext.Tables.FindAsync([dto.Id], cancellationToken)
              ?? throw new MetaDataException($"表不存在：{dto.Id}")
            : new TableMetadata
            {
                ConnectionId = dto.ConnectionId,
                TableName = dto.TableName.Trim(),
                Schema = dto.Schema?.Trim(),
                CreatedAt = DateTime.Now
            };

        if (dto.Id == 0)
        {
            var connExists = await _dbContext.Connections.AsNoTracking().AnyAsync(x => x.Id == dto.ConnectionId, cancellationToken);
            if (!connExists)
            {
                throw new MetaDataException($"连接不存在：{dto.ConnectionId}");
            }
        }

        entity.DisplayName = dto.DisplayName?.Trim();
        entity.IsPublished = dto.IsPublished;
        entity.DefaultSortField = dto.DefaultSortField?.Trim();
        entity.Remark = dto.Remark;
        entity.UpdatedAt = DateTime.Now;

        if (dto.Id == 0)
        {
            _dbContext.Tables.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task SaveFieldsAsync(SaveFieldsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Fields.Count == 0)
        {
            return;
        }

        var tableExists = await _dbContext.Tables.AsNoTracking().AnyAsync(x => x.Id == request.TableId, cancellationToken);
        if (!tableExists)
        {
            throw new MetaDataException($"表不存在：{request.TableId}");
        }

        // 请求内别名唯一校验
        var duplicateAlias = request.Fields
            .GroupBy(f => f.Alias?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateAlias is not null)
        {
            throw new MetaDataException($"字段别名存在重复：{duplicateAlias.Key}");
        }

        var ids = request.Fields.Select(f => f.Id).ToHashSet();
        var stored = await _dbContext.Fields.Where(x => x.TableId == request.TableId && ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (stored.Count != request.Fields.Count)
        {
            throw new MetaDataException("部分字段不存在或不属于该表，保存中止。");
        }

        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dto in request.Fields)
        {
            var alias = dto.Alias?.Trim() ?? string.Empty;
            if (alias.Length > 64 || !AliasPattern.IsMatch(alias))
            {
                throw new MetaDataException($"别名“{alias}”不合法：需以字母或下划线开头，仅含字母数字下划线，长度不超过 64。");
            }

            if (!aliases.Add(alias))
            {
                throw new MetaDataException($"字段别名存在重复：{alias}");
            }

            var entity = stored.First(x => x.Id == dto.Id);
            entity.Alias = alias;
            entity.DisplayName = dto.DisplayName?.Trim();
            entity.Ordinal = dto.Ordinal;
            entity.IsVisible = dto.IsVisible;
            entity.DataCategory = dto.DataCategory;
            entity.Remark = dto.Remark;
            entity.UpdatedAt = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishAsync(PublishTableRequest request, CancellationToken cancellationToken = default)
    {
        var table = await _dbContext.Tables.FindAsync([request.TableId], cancellationToken)
                    ?? throw new MetaDataException($"表不存在：{request.TableId}");
        table.IsPublished = request.IsPublished;
        table.UpdatedAt = DateTime.Now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTableAsync(long id, CancellationToken cancellationToken = default)
    {
        var table = await _dbContext.Tables.FindAsync([id], cancellationToken)
                    ?? throw new MetaDataException($"表不存在：{id}");
        _dbContext.Tables.Remove(table);
        // 用户偏好无外键约束，手动清理
        var prefs = _dbContext.UserFieldPreferences.Where(x => x.TableId == id);
        _dbContext.UserFieldPreferences.RemoveRange(prefs);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static FieldDto ToDto(FieldMetadata f) => new()
    {
        Id = f.Id,
        TableId = f.TableId,
        FieldName = f.FieldName,
        Alias = f.Alias,
        DisplayName = f.DisplayName,
        Ordinal = f.Ordinal,
        IsVisible = f.IsVisible,
        NativeDataType = f.NativeDataType,
        DataCategory = f.DataCategory,
        MaxLength = f.MaxLength,
        NumericPrecision = f.NumericPrecision,
        NumericScale = f.NumericScale,
        IsNullable = f.IsNullable,
        IsPrimaryKey = f.IsPrimaryKey,
        Remark = f.Remark
    };
}

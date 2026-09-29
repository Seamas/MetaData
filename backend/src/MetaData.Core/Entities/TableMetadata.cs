namespace MetaData.Core.Entities;

/// <summary>业务表元数据。</summary>
public class TableMetadata
{
    public long Id { get; set; }

    public long ConnectionId { get; set; }

    /// <summary>Schema/Owner/模式名，可空（如 MySQL 库内表无独立 schema）。</summary>
    public string? Schema { get; set; }

    /// <summary>物理表名。</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>中文显示名。</summary>
    public string? DisplayName { get; set; }

    /// <summary>管理员是否开放给业务数据查询。</summary>
    public bool IsPublished { get; set; }

    /// <summary>默认排序字段（物理字段名，可空）。</summary>
    public string? DefaultSortField { get; set; }

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DbConnectionInfo? Connection { get; set; }

    public ICollection<FieldMetadata> Fields { get; set; } = new List<FieldMetadata>();
}

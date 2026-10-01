using MetaData.Abstractions.Enums;
using Wang.Seamas.Shared.Entities;

namespace MetaData.Core.Entities;

/// <summary>业务表字段元数据。</summary>
public class FieldMetadata : BaseEntity<long>
{
    public long TableId { get; set; }

    /// <summary>物理字段名。</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>暴露给前端的别名，表内唯一；默认等于字段名。</summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>显示名（中文名）。</summary>
    public string? DisplayName { get; set; }

    /// <summary>字段顺序。</summary>
    public int Ordinal { get; set; }

    /// <summary>是否默认显示。</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>原始类型描述，如 varchar(100)、NUMBER(10,2)。</summary>
    public string? NativeDataType { get; set; }

    /// <summary>归一化数据分类。</summary>
    public DataCategory DataCategory { get; set; } = DataCategory.Unknown;

    public int? MaxLength { get; set; }

    public int? NumericPrecision { get; set; }

    public int? NumericScale { get; set; }

    public bool IsNullable { get; set; } = true;

    public bool IsPrimaryKey { get; set; }

    public string? Remark { get; set; }

    public TableMetadata? Table { get; set; }
}

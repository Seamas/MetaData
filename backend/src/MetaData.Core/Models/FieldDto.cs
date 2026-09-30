using MetaData.Abstractions.Enums;

namespace MetaData.Core.Models;

/// <summary>字段元数据 DTO。</summary>
public class FieldDto
{
    public long Id { get; set; }

    public long TableId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public int Ordinal { get; set; }

    public bool IsVisible { get; set; } = true;

    public string? NativeDataType { get; set; }

    public DataCategory DataCategory { get; set; } = DataCategory.Unknown;

    public int? MaxLength { get; set; }

    public int? NumericPrecision { get; set; }

    public int? NumericScale { get; set; }

    public bool IsNullable { get; set; } = true;

    public bool IsPrimaryKey { get; set; }

    public string? Remark { get; set; }
}

using MetaData.Abstractions.Enums;

namespace MetaData.Core.Models;

/// <summary>查询结果列描述（对前端暴露别名，不暴露物理字段名）。</summary>
public class ColumnDto
{
    public string Alias { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public DataCategory DataCategory { get; set; }

    public string? NativeDataType { get; set; }

    public int? Width { get; set; }

    public bool IsPrimaryKey { get; set; }
}

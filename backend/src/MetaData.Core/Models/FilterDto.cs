using System.Text.Json;
using MetaData.Abstractions.Enums;

namespace MetaData.Core.Models;

/// <summary>业务数据查询的一个过滤条件。</summary>
public class FilterDto
{
    /// <summary>字段别名。</summary>
    public string Alias { get; set; } = string.Empty;

    public FilterOperator Operator { get; set; }

    /// <summary>比较值（JSON 原始值，由后端按字段类型转换）。</summary>
    public JsonElement? Value { get; set; }

    /// <summary>区间查询的第二值。</summary>
    public JsonElement? Value2 { get; set; }
}

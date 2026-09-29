using System.Text.Json;
using MetaData.Core.Enums;

namespace MetaData.Core.Models;

public class PublishedTableDto
{
    public long TableId { get; set; }

    public long ConnectionId { get; set; }

    public string ConnectionName { get; set; } = string.Empty;

    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}

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

public class SortDto
{
    public string Alias { get; set; } = string.Empty;

    public SortDirection Direction { get; set; } = SortDirection.Asc;
}

public class DataQueryRequest
{
    public long TableId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public List<FilterDto> Filters { get; set; } = [];

    public List<SortDto> Sorts { get; set; } = [];
}

public class ColumnDto
{
    public string Alias { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public DataCategory DataCategory { get; set; }

    public string? NativeDataType { get; set; }

    public int? Width { get; set; }

    public bool IsPrimaryKey { get; set; }
}

public class DataQueryResponse
{
    public List<ColumnDto> Columns { get; set; } = [];

    /// <summary>行数据：键为字段别名。</summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    public long Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}

public class FieldPreferenceDto
{
    public long FieldId { get; set; }

    public int Ordinal { get; set; }

    public bool IsVisible { get; set; } = true;

    public int? Width { get; set; }
}

public class SavePreferencesRequest
{
    public long TableId { get; set; }

    public List<FieldPreferenceDto> Items { get; set; } = [];
}

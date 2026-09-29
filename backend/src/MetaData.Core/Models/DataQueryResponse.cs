namespace MetaData.Core.Models;

/// <summary>业务数据查询结果。</summary>
public class DataQueryResponse
{
    public List<ColumnDto> Columns { get; set; } = [];

    /// <summary>行数据：键为字段别名。</summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    public long Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}

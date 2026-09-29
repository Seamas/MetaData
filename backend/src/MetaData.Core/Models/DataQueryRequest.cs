namespace MetaData.Core.Models;

/// <summary>业务数据查询请求。</summary>
public class DataQueryRequest
{
    public long TableId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public List<FilterDto> Filters { get; set; } = [];

    public List<SortDto> Sorts { get; set; } = [];
}

using MyWebProject.Shared.DTOs;

namespace MetaData.Application.Models;

/// <summary>业务数据查询请求（分页参数继承公共 <see cref="PagedQuery"/>）。</summary>
public class DataQueryRequest : PagedQuery
{
    public long TableId { get; set; }

    public List<FilterDto> Filters { get; set; } = [];

    public List<SortDto> Sorts { get; set; } = [];
}

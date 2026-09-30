using MyWebProject.Shared.DTOs;

namespace MetaData.Application.Models;

/// <summary>业务数据查询结果（分页结构继承公共 <see cref="PagedResult{T}"/>，Items 键为字段别名）。</summary>
public class DataQueryResponse : PagedResult<Dictionary<string, object?>>
{
    public List<ColumnDto> Columns { get; set; } = [];
}

using MetaData.Abstractions.Enums;

namespace MetaData.Application.Models;

/// <summary>业务数据查询的一个排序项。</summary>
public class SortDto
{
    public string Alias { get; set; } = string.Empty;

    public SortDirection Direction { get; set; } = SortDirection.Asc;
}

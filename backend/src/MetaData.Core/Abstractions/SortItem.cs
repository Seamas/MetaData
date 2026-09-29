using MetaData.Core.Enums;

namespace MetaData.Core.Abstractions;

/// <summary>一个排序项。</summary>
public class SortItem
{
    public required string ColumnName { get; init; }

    public SortDirection Direction { get; init; } = SortDirection.Asc;
}

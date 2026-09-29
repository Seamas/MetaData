namespace MetaData.Core.Abstractions;

/// <summary>单表查询的组成部分，供方言生成 SQL。</summary>
public class QueryParts
{
    public string? Schema { get; init; }

    public required string TableName { get; init; }

    /// <summary>需要输出的物理列名。</summary>
    public required IReadOnlyList<string> SelectColumns { get; init; }

    public IReadOnlyList<FilterCondition> Filters { get; init; } = [];

    public IReadOnlyList<SortItem> Sorts { get; init; } = [];

    public long Skip { get; init; }

    public int Take { get; init; }
}

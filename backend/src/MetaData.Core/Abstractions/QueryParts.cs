using MetaData.Core.Enums;

namespace MetaData.Core.Abstractions;

/// <summary>一个过滤条件（值已转换为目标 CLR 类型）。</summary>
public class FilterCondition
{
    /// <summary>物理列名（未加引用符）。</summary>
    public required string ColumnName { get; init; }

    public FilterOperator Operator { get; init; }

    public object? Value { get; init; }

    public object? Value2 { get; init; }
}

/// <summary>一个排序项。</summary>
public class SortItem
{
    public required string ColumnName { get; init; }

    public SortDirection Direction { get; init; } = SortDirection.Asc;
}

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

/// <summary>生成好的 SQL 及命名参数（参数名不含前缀符号）。</summary>
public record BuiltSql(string Sql, IReadOnlyDictionary<string, object?> Parameters);

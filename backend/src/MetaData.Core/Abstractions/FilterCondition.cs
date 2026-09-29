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

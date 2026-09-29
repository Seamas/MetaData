namespace MetaData.Core.Enums;

/// <summary>过滤条件操作符。</summary>
public enum FilterOperator
{
    Equal = 1,
    NotEqual = 2,
    Contains = 3,
    StartsWith = 4,
    EndsWith = 5,
    GreaterThan = 6,
    GreaterThanOrEqual = 7,
    LessThan = 8,
    LessThanOrEqual = 9,
    Between = 10,
    IsNull = 11,
    IsNotNull = 12
}

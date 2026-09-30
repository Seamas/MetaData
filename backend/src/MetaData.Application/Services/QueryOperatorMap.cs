using MetaData.Abstractions.Enums;
using MetaData.Application.Models;

namespace MetaData.Application.Services;

/// <summary>数据分类与查询操作符的常量映射（前后端共用的唯一事实来源）。</summary>
public static class QueryOperatorMap
{
    public static readonly IReadOnlyDictionary<DataCategory, string> CategoryLabels =
        new Dictionary<DataCategory, string>
        {
            [DataCategory.String] = "字符串",
            [DataCategory.Number] = "数字",
            [DataCategory.DateTime] = "日期时间",
            [DataCategory.Boolean] = "布尔",
            [DataCategory.Guid] = "唯一标识",
            [DataCategory.Binary] = "二进制",
            [DataCategory.Unknown] = "其他"
        };

    public static readonly IReadOnlyDictionary<FilterOperator, (string Label, bool NeedsValue, bool NeedsValue2)> OperatorInfo =
        new Dictionary<FilterOperator, (string, bool, bool)>
        {
            [FilterOperator.Equal] = ("等于", true, false),
            [FilterOperator.NotEqual] = ("不等于", true, false),
            [FilterOperator.Contains] = ("包含", true, false),
            [FilterOperator.StartsWith] = ("开头是", true, false),
            [FilterOperator.EndsWith] = ("结尾是", true, false),
            [FilterOperator.GreaterThan] = ("大于", true, false),
            [FilterOperator.GreaterThanOrEqual] = ("大于等于", true, false),
            [FilterOperator.LessThan] = ("小于", true, false),
            [FilterOperator.LessThanOrEqual] = ("小于等于", true, false),
            [FilterOperator.Between] = ("区间", true, true),
            [FilterOperator.IsNull] = ("为空", false, false),
            [FilterOperator.IsNotNull] = ("非空", false, false)
        };

    public static readonly IReadOnlyDictionary<DataCategory, List<FilterOperator>> CategoryOperators =
        new Dictionary<DataCategory, List<FilterOperator>>
        {
            [DataCategory.String] =
            [
                FilterOperator.Contains, FilterOperator.Equal, FilterOperator.NotEqual,
                FilterOperator.StartsWith, FilterOperator.EndsWith, FilterOperator.IsNull, FilterOperator.IsNotNull
            ],
            [DataCategory.Number] =
            [
                FilterOperator.Equal, FilterOperator.NotEqual, FilterOperator.GreaterThan,
                FilterOperator.GreaterThanOrEqual, FilterOperator.LessThan, FilterOperator.LessThanOrEqual,
                FilterOperator.Between, FilterOperator.IsNull, FilterOperator.IsNotNull
            ],
            [DataCategory.DateTime] =
            [
                FilterOperator.Equal, FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEqual,
                FilterOperator.LessThan, FilterOperator.LessThanOrEqual, FilterOperator.Between,
                FilterOperator.IsNull, FilterOperator.IsNotNull
            ],
            [DataCategory.Boolean] = [FilterOperator.Equal, FilterOperator.IsNull, FilterOperator.IsNotNull],
            [DataCategory.Guid] = [FilterOperator.Equal, FilterOperator.IsNull, FilterOperator.IsNotNull]
        };

    public static bool IsOperatorAllowed(DataCategory category, FilterOperator op)
        => CategoryOperators.TryGetValue(category, out var list) && list.Contains(op);

    public static List<CategoryOperators> BuildDescriptorList()
        => CategoryOperators.Select(kv => new CategoryOperators
        {
            DataCategory = kv.Key,
            Label = CategoryLabels.GetValueOrDefault(kv.Key, kv.Key.ToString()),
            Operators = kv.Value.Select(op =>
            {
                var info = OperatorInfo[op];
                return new OperatorDescriptor
                {
                    Operator = op,
                    Label = info.Label,
                    NeedsValue = info.NeedsValue,
                    NeedsValue2 = info.NeedsValue2
                };
            }).ToList()
        }).ToList();
}

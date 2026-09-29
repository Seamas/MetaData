using MetaData.Core.Enums;

namespace MetaData.Core.Models;

/// <summary>数据分类支持的操作符集合。</summary>
public class CategoryOperators
{
    public DataCategory DataCategory { get; set; }

    public string Label { get; set; } = string.Empty;

    public List<OperatorDescriptor> Operators { get; set; } = [];
}

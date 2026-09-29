using MetaData.Core.Enums;

namespace MetaData.Core.Models;

/// <summary>操作符描述（前端据此渲染条件控件）。</summary>
public class OperatorDescriptor
{
    public FilterOperator Operator { get; set; }

    public string Label { get; set; } = string.Empty;

    /// <summary>是否需要第一个值。</summary>
    public bool NeedsValue { get; set; }

    /// <summary>是否需要第二个值（区间）。</summary>
    public bool NeedsValue2 { get; set; }
}

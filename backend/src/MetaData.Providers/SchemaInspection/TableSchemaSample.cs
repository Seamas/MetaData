using MetaData.Abstractions.Schema;

namespace MetaData.Providers.SchemaInspection;

/// <summary>表结构样本的默认实现（仅 Providers 内部可见，公共层只依赖接口）。</summary>
internal sealed class TableSchemaSample : ITableSchemaSample
{
    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string? Comment { get; set; }
}

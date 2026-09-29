namespace MetaData.Core.SchemaInspection;

/// <summary>从业务库读取到的表结构样本。</summary>
public class TableSchemaSample
{
    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string? Comment { get; set; }
}

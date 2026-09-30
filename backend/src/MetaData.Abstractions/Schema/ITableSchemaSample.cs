namespace MetaData.Abstractions.Schema;

/// <summary>从业务库读取到的表结构样本。</summary>
public interface ITableSchemaSample
{
    string? Schema { get; }

    string TableName { get; }

    string? Comment { get; }
}

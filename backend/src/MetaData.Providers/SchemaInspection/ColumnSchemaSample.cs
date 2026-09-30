using MetaData.Abstractions.Schema;

namespace MetaData.Providers.SchemaInspection;

/// <summary>字段结构样本的默认实现（仅 Providers 内部可见，公共层只依赖接口）。</summary>
internal sealed class ColumnSchemaSample : IColumnSchemaSample
{
    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string ColumnName { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    /// <summary>基础类型名，如 varchar、NUMBER。</summary>
    public string DataTypeName { get; set; } = string.Empty;

    /// <summary>完整类型描述，如 varchar(100)、NUMBER(10,2)。</summary>
    public string? NativeDataType { get; set; }

    public int? MaxLength { get; set; }

    public int? NumericPrecision { get; set; }

    public int? NumericScale { get; set; }

    public bool IsNullable { get; set; }

    public bool IsPrimaryKey { get; set; }

    public string? Comment { get; set; }
}

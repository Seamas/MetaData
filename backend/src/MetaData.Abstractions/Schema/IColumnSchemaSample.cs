namespace MetaData.Abstractions.Schema;

/// <summary>从业务库读取到的字段结构样本。</summary>
public interface IColumnSchemaSample
{
    string? Schema { get; }

    string TableName { get; }

    string ColumnName { get; }

    int Ordinal { get; }

    /// <summary>基础类型名，如 varchar、NUMBER。</summary>
    string DataTypeName { get; }

    /// <summary>完整类型描述，如 varchar(100)、NUMBER(10,2)。</summary>
    string? NativeDataType { get; }

    int? MaxLength { get; }

    int? NumericPrecision { get; }

    int? NumericScale { get; }

    bool IsNullable { get; }

    bool IsPrimaryKey { get; }

    string? Comment { get; }
}

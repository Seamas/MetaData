using MetaData.Abstractions.Enums;

namespace MetaData.Abstractions;

/// <summary>
/// 数据库方言：负责标识符引用、参数风格、版本探测、分页 SQL、数据类型映射、连接串拼装。
/// 不依赖任何具体 ADO.NET 驱动，可在无驱动环境下单测。
/// </summary>
public interface IDatabaseDialect
{
    DatabaseType DatabaseType { get; }

    /// <summary>命名参数前缀，如 @ 或 :。</summary>
    char ParameterPrefix { get; }

    /// <summary>给标识符加引用符。</summary>
    string QuoteIdentifier(string name);

    /// <summary>限定表名（schema.table）。</summary>
    string QualifyTable(string? schema, string tableName);

    /// <summary>版本探测 SQL（返回单行单列）。</summary>
    string GetVersionSql();

    /// <summary>解析版本探测结果。</summary>
    DbVersionInfo ParseVersion(string raw);

    /// <summary>Count 查询。</summary>
    BuiltSql BuildCountQuery(QueryParts parts);

    /// <summary>分页查询（方言内部按版本选择语法）。</summary>
    BuiltSql BuildPagedQuery(QueryParts parts, DbVersionInfo version);

    /// <summary>原始数据类型名映射为归一化分类。</summary>
    DataCategory MapDataType(string? dataTypeName, string? fullNativeType, int? numericPrecision, int? numericScale);

    /// <summary>由结构化连接设置拼出该驱动的连接串；Advanced 模式解密直返。</summary>
    string BuildConnectionString(ConnectionSettings settings);
}

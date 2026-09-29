using System.Data.Common;
using MetaData.Core.Enums;

namespace MetaData.Core.SchemaInspection;

/// <summary>指定数据库的系统结构读取器（各库系统视图实现不同）。</summary>
public interface ISchemaInspector
{
    DatabaseType DatabaseType { get; }

    /// <summary>查询数据库版本原文。</summary>
    Task<string> GetVersionRawAsync(DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>读取用户表清单（schema 为空时由各库决定默认范围）。</summary>
    Task<IReadOnlyList<TableSchemaSample>> GetTablesAsync(DbConnection connection, string? schema, CancellationToken cancellationToken = default);

    /// <summary>读取指定表的字段结构。</summary>
    Task<IReadOnlyList<ColumnSchemaSample>> GetColumnsAsync(
        DbConnection connection,
        string? schema,
        IReadOnlyCollection<TableSchemaSample> tables,
        CancellationToken cancellationToken = default);
}

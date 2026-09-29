using MetaData.Core.Dialects;
using MetaData.Core.Enums;

namespace MetaData.Core.SchemaInspection;

/// <summary>SQL Server 结构读取（INFORMATION_SCHEMA，2012+ 通用）。</summary>
public class SqlServerSchemaInspector : SchemaInspectorBase
{
    public SqlServerSchemaInspector(SqlServerDialect dialect) : base(dialect)
    {
    }

    public override DatabaseType DatabaseType => DatabaseType.SqlServer;

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildTablesCommand(string? schema)
    {
        const string sql = """
            SELECT t.TABLE_SCHEMA AS Schema, t.TABLE_NAME AS TableName, '' AS Comment
            FROM INFORMATION_SCHEMA.TABLES t
            WHERE t.TABLE_TYPE = 'BASE TABLE'
              AND (@s IS NULL OR t.TABLE_SCHEMA = @s)
            ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME
            """;
        return (sql, new[] { ("s", (object?)schema) });
    }

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildColumnsCommand(
        IReadOnlyCollection<TableSchemaSample> tables)
    {
        var (whereSql, ps) = BuildTablePairs(tables, "c.TABLE_SCHEMA", "c.TABLE_NAME");

        var sql = $"""
            SELECT c.TABLE_SCHEMA AS Schema, c.TABLE_NAME AS TableName, c.COLUMN_NAME AS ColumnName,
                   c.ORDINAL_POSITION AS Ordinal, c.DATA_TYPE AS DataTypeName,
                   CASE
                     WHEN c.CHARACTER_MAXIMUM_LENGTH IS NOT NULL
                          AND c.DATA_TYPE IN ('varchar','char','nvarchar','nchar','varbinary','binary')
                     THEN c.DATA_TYPE + '(' + CASE WHEN c.CHARACTER_MAXIMUM_LENGTH = -1 THEN 'max'
                                                   ELSE CAST(c.CHARACTER_MAXIMUM_LENGTH AS varchar(20)) END + ')'
                     ELSE c.DATA_TYPE
                   END AS FullType,
                   c.CHARACTER_MAXIMUM_LENGTH AS MaxLength, c.NUMERIC_PRECISION AS Prec, c.NUMERIC_SCALE AS Scale,
                   c.IS_NULLABLE AS IsNullableText, '' AS Comment,
                   CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPk
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT kcu.TABLE_SCHEMA, kcu.TABLE_NAME, kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                 AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA AND tc.TABLE_NAME = kcu.TABLE_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA AND pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
            WHERE {whereSql}
            ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
            """;
        return (sql, ps);
    }
}

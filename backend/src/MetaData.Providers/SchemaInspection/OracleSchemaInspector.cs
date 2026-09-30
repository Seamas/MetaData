using MetaData.Providers.Dialects;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Schema;

namespace MetaData.Providers.SchemaInspection;

/// <summary>Oracle 结构读取（ALL_TABLES/ALL_TAB_COLUMNS/ALL_COL_COMMENTS/ALL_CONSTRAINTS，11g+ 通用）。</summary>
public class OracleSchemaInspector : SchemaInspectorBase
{
    public OracleSchemaInspector(OracleDialect dialect) : base(dialect)
    {
    }

    public override DatabaseType DatabaseType => DatabaseType.Oracle;

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildTablesCommand(string? schema)
    {
        // 未指定 schema 时默认当前登录用户 OWNER = USER
        var sql = """
            SELECT t.OWNER AS Schema, t.TABLE_NAME AS TableName, COALESCE(c.COMMENTS, '') AS Comment
            FROM ALL_TABLES t
            LEFT JOIN ALL_TAB_COMMENTS c ON c.OWNER = t.OWNER AND c.TABLE_NAME = t.TABLE_NAME
            WHERE t.NESTED = 'NO' AND t.SECONDARY = 'N'
              AND (:s IS NULL AND t.OWNER = USER OR :s IS NOT NULL AND t.OWNER = :s)
            ORDER BY t.OWNER, t.TABLE_NAME
            """;
        return (sql, new[] { ("s", (object?)schema) });
    }

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildColumnsCommand(
        IReadOnlyCollection<ITableSchemaSample> tables)
    {
        var (whereSql, ps) = BuildTablePairs(tables, "c.OWNER", "c.TABLE_NAME");

        var sql = $"""
            SELECT c.OWNER AS Schema, c.TABLE_NAME AS TableName, c.COLUMN_NAME AS ColumnName,
                   c.COLUMN_ID AS Ordinal, c.DATA_TYPE AS DataTypeName,
                   c.DATA_TYPE ||
                     CASE
                       WHEN c.DATA_TYPE IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','RAW')
                       THEN '(' || CASE WHEN c.CHAR_USED = 'C' THEN c.CHAR_LENGTH ELSE c.DATA_LENGTH END
                            || CASE WHEN c.CHAR_USED = 'C' THEN ' CHAR' ELSE '' END || ')'
                       WHEN c.DATA_TYPE = 'NUMBER' AND c.DATA_PRECISION IS NOT NULL
                       THEN '(' || c.DATA_PRECISION || ',' || NVL(c.DATA_SCALE, 0) || ')'
                       ELSE ''
                     END AS FullType,
                   c.DATA_LENGTH AS MaxLength, c.DATA_PRECISION AS Prec, c.DATA_SCALE AS Scale,
                   CASE c.NULLABLE WHEN 'Y' THEN 'YES' ELSE 'NO' END AS IsNullableText,
                   cm.COMMENTS AS Comment,
                   CASE WHEN pk.POSITION IS NOT NULL THEN 1 ELSE 0 END AS IsPk
            FROM ALL_TAB_COLUMNS c
            LEFT JOIN ALL_COL_COMMENTS cm
              ON cm.OWNER = c.OWNER AND cm.TABLE_NAME = c.TABLE_NAME AND cm.COLUMN_NAME = c.COLUMN_NAME
            LEFT JOIN (
                SELECT cc.OWNER, cc.TABLE_NAME, cc.COLUMN_NAME, cc.POSITION
                FROM ALL_CONSTRAINTS con
                JOIN ALL_CONS_COLUMNS cc
                  ON cc.OWNER = con.OWNER AND cc.CONSTRAINT_NAME = con.CONSTRAINT_NAME
                WHERE con.CONSTRAINT_TYPE = 'P'
            ) pk ON pk.OWNER = c.OWNER AND pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
            WHERE {whereSql}
            ORDER BY c.OWNER, c.TABLE_NAME, c.COLUMN_ID
            """;
        return (sql, ps);
    }
}

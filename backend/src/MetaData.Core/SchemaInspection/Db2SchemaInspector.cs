using MetaData.Core.Dialects;
using MetaData.Core.Enums;

namespace MetaData.Core.SchemaInspection;

/// <summary>DB2 for LUW 结构读取（SYSCAT.TABLES/COLUMNS，10.x/11.x/12.x 通用；TABSCHEMA 为定长需 RTRIM）。</summary>
public class Db2SchemaInspector : SchemaInspectorBase
{
    public Db2SchemaInspector(Db2Dialect dialect) : base(dialect)
    {
    }

    public override DatabaseType DatabaseType => DatabaseType.Db2;

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildTablesCommand(string? schema)
    {
        var sql = """
            SELECT RTRIM(t.TABSCHEMA) AS Schema, t.TABNAME AS TableName, COALESCE(t.REMARKS, '') AS Comment
            FROM SYSCAT.TABLES t
            WHERE t.TYPE = 'T'
              AND (
                (@s IS NOT NULL AND RTRIM(t.TABSCHEMA) = @s)
                OR (@s IS NULL AND RTRIM(t.TABSCHEMA) NOT LIKE 'SYS%' AND RTRIM(t.TABSCHEMA) NOT LIKE 'SQL%')
              )
            ORDER BY t.TABSCHEMA, t.TABNAME
            """;
        return (sql, new[] { ("s", (object?)schema) });
    }

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildColumnsCommand(
        IReadOnlyCollection<TableSchemaSample> tables)
    {
        var (whereSql, ps) = BuildTablePairs(tables, "RTRIM(c.TABSCHEMA)", "c.TABNAME");

        var sql = $"""
            SELECT RTRIM(c.TABSCHEMA) AS Schema, c.TABNAME AS TableName, c.COLNAME AS ColumnName,
                   c.COLNO + 1 AS Ordinal, c.TYPENAME AS DataTypeName,
                   c.TYPENAME ||
                     CASE
                       WHEN c.TYPENAME IN ('VARCHAR','CHARACTER','CHAR','VARGRAPHIC','GRAPHIC','CLOB','BLOB','DBCLOB')
                       THEN '(' || c.LENGTH || ')'
                       WHEN c.TYPENAME IN ('DECIMAL','DECFLOAT')
                       THEN '(' || c.LENGTH || ',' || COALESCE(c.SCALE, 0) || ')'
                       ELSE ''
                     END AS FullType,
                   c.LENGTH AS MaxLength, c.LENGTH AS Prec, c.SCALE AS Scale,
                   CASE c.NULLS WHEN 'Y' THEN 'YES' ELSE 'NO' END AS IsNullableText,
                   c.REMARKS AS Comment,
                   CASE WHEN c.KEYSEQ IS NOT NULL THEN 1 ELSE 0 END AS IsPk
            FROM SYSCAT.COLUMNS c
            WHERE {whereSql}
            ORDER BY c.TABSCHEMA, c.TABNAME, c.COLNO
            """;
        return (sql, ps);
    }
}

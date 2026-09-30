using MetaData.Providers.Dialects;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Schema;

namespace MetaData.Providers.SchemaInspection;

/// <summary>PostgreSQL 结构读取（information_schema + pg_catalog 注释/主键，12+ 通用）。</summary>
public class PostgreSqlSchemaInspector : SchemaInspectorBase
{
    public PostgreSqlSchemaInspector(PostgreSqlDialect dialect) : base(dialect)
    {
    }

    public override DatabaseType DatabaseType => DatabaseType.PostgreSql;

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildTablesCommand(string? schema)
    {
        const string sql = """
            SELECT t.table_schema AS Schema, t.table_name AS TableName,
                   COALESCE(obj_description(format('%I.%I', t.table_schema, t.table_name)::regclass, 'pg_class'), '') AS Comment
            FROM information_schema.tables t
            WHERE t.table_type = 'BASE TABLE'
              AND (
                (@s IS NOT NULL AND t.table_schema = @s)
                OR (@s IS NULL AND t.table_schema NOT IN ('pg_catalog', 'information_schema')
                     AND t.table_schema NOT LIKE 'pg_toast%')
              )
            ORDER BY t.table_schema, t.table_name
            """;
        return (sql, new[] { ("s", (object?)schema) });
    }

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildColumnsCommand(
        IReadOnlyCollection<ITableSchemaSample> tables)
    {
        var (whereSql, ps) = BuildTablePairs(tables, "c.table_schema", "c.table_name");

        var sql = $"""
            SELECT c.table_schema AS Schema, c.table_name AS TableName, c.column_name AS ColumnName,
                   c.ordinal_position AS Ordinal, c.data_type AS DataTypeName, c.data_type AS FullType,
                   c.character_maximum_length AS MaxLength, c.numeric_precision AS Prec, c.numeric_scale AS Scale,
                   c.is_nullable AS IsNullableText,
                   col_description(format('%I.%I', c.table_schema, c.table_name)::regclass, c.ordinal_position) AS Comment,
                   CASE WHEN pk.column_name IS NOT NULL THEN 1 ELSE 0 END AS IsPk
            FROM information_schema.columns c
            LEFT JOIN (
                SELECT kcu.table_schema, kcu.table_name, kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.table_schema = kcu.table_schema AND tc.table_name = kcu.table_name
                WHERE tc.constraint_type = 'PRIMARY KEY'
            ) pk ON pk.table_schema = c.table_schema AND pk.table_name = c.table_name AND pk.column_name = c.column_name
            WHERE {whereSql}
            ORDER BY c.table_schema, c.table_name, c.ordinal_position
            """;
        return (sql, ps);
    }
}

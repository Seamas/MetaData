using MetaData.Providers.Dialects;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Schema;

namespace MetaData.Providers.SchemaInspection;

/// <summary>MySQL 结构读取（information_schema，5.7/8.0 通用；表均位于当前连接库 DATABASE() 下）。</summary>
public class MySqlSchemaInspector : SchemaInspectorBase
{
    public MySqlSchemaInspector(MySqlDialect dialect) : base(dialect)
    {
    }

    public override DatabaseType DatabaseType => DatabaseType.MySql;

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildTablesCommand(string? schema)
    {
        const string sql = """
            SELECT NULL AS Schema, TABLE_NAME AS TableName, COALESCE(TABLE_COMMENT, '') AS Comment
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME
            """;
        return (sql, []);
    }

    protected override (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildColumnsCommand(
        IReadOnlyCollection<ITableSchemaSample> tables)
    {
        var conditions = new List<string>();
        var parameters = new List<(string, object?)>();
        var i = 0;
        foreach (var t in tables)
        {
            var name = "t" + i;
            conditions.Add($"c.TABLE_NAME = {Dialect.ParameterPrefix}{name}");
            parameters.Add((name, t.TableName));
            i++;
        }

        var sql = $"""
            SELECT NULL AS Schema, c.TABLE_NAME AS TableName, c.COLUMN_NAME AS ColumnName,
                   c.ORDINAL_POSITION AS Ordinal, c.DATA_TYPE AS DataTypeName, c.COLUMN_TYPE AS FullType,
                   c.CHARACTER_MAXIMUM_LENGTH AS MaxLength, c.NUMERIC_PRECISION AS Prec, c.NUMERIC_SCALE AS Scale,
                   c.IS_NULLABLE AS IsNullableText, COALESCE(c.COLUMN_COMMENT, '') AS Comment,
                   CASE WHEN c.COLUMN_KEY = 'PRI' THEN 1 ELSE 0 END AS IsPk
            FROM information_schema.COLUMNS c
            WHERE c.TABLE_SCHEMA = DATABASE() AND ({string.Join(" OR ", conditions)})
            ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION
            """;
        return (sql, parameters);
    }
}

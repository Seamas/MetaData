using System.Data.Common;
using MetaData.Core.Abstractions;
using MetaData.Core.Enums;

namespace MetaData.Core.SchemaInspection;

/// <summary>结构读取器基类：统一命令创建、参数绑定与结果物化（结果列别名固定）。</summary>
public abstract class SchemaInspectorBase : ISchemaInspector
{
    protected SchemaInspectorBase(IDatabaseDialect dialect)
    {
        Dialect = dialect;
    }

    protected IDatabaseDialect Dialect { get; }

    public abstract DatabaseType DatabaseType { get; }

    protected abstract (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildTablesCommand(string? schema);

    protected abstract (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) BuildColumnsCommand(
        IReadOnlyCollection<TableSchemaSample> tables);

    public async Task<string> GetVersionRawAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, Dialect.GetVersionSql(), []);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString() ?? string.Empty;
    }

    public async Task<IReadOnlyList<TableSchemaSample>> GetTablesAsync(
        DbConnection connection, string? schema, CancellationToken cancellationToken = default)
    {
        var (sql, ps) = BuildTablesCommand(string.IsNullOrWhiteSpace(schema) ? null : schema.Trim());
        await using var command = CreateCommand(connection, sql, ps);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<TableSchemaSample>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TableSchemaSample
            {
                Schema = reader.GetNullableString(reader.GetOrdinal("Schema")),
                TableName = reader.GetString(reader.GetOrdinal("TableName")),
                Comment = reader.GetNullableString(reader.GetOrdinal("Comment"))
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<ColumnSchemaSample>> GetColumnsAsync(
        DbConnection connection,
        string? schema,
        IReadOnlyCollection<TableSchemaSample> tables,
        CancellationToken cancellationToken = default)
    {
        if (tables.Count == 0)
        {
            return [];
        }

        var (sql, ps) = BuildColumnsCommand(tables);
        await using var command = CreateCommand(connection, sql, ps);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<ColumnSchemaSample>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var maxLength = reader.GetNullableInt32(reader.GetOrdinal("MaxLength"));
            result.Add(new ColumnSchemaSample
            {
                Schema = reader.GetNullableString(reader.GetOrdinal("Schema")),
                TableName = reader.GetString(reader.GetOrdinal("TableName")),
                ColumnName = reader.GetString(reader.GetOrdinal("ColumnName")),
                Ordinal = reader.GetInt32(reader.GetOrdinal("Ordinal")),
                DataTypeName = reader.GetString(reader.GetOrdinal("DataTypeName")),
                NativeDataType = reader.GetNullableString(reader.GetOrdinal("FullType")),
                MaxLength = maxLength is null or < 0 ? null : maxLength,
                NumericPrecision = reader.GetNullableInt32(reader.GetOrdinal("Prec")),
                NumericScale = reader.GetNullableInt32(reader.GetOrdinal("Scale")),
                IsNullable = reader.GetNullableString(reader.GetOrdinal("IsNullableText")) is "YES" or "Y",
                Comment = reader.GetNullableString(reader.GetOrdinal("Comment")),
                IsPrimaryKey = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("IsPk"))) == 1
            });
        }

        return result;
    }

    protected DbCommand CreateCommand(DbConnection connection, string sql, IReadOnlyList<(string Name, object? Value)> parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = Dialect.ParameterPrefix + name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    /// <summary>构造 (schema 列, table 列) 的成对 OR 条件与参数；无 schema 信息时仅按表名匹配。</summary>
    protected (string WhereSql, List<(string Name, object? Value)> Parameters) BuildTablePairs(
        IReadOnlyCollection<TableSchemaSample> tables, string schemaColumn, string tableColumn)
    {
        var parameters = new List<(string, object?)>();
        var conditions = new List<string>();
        var index = 0;
        var useSchema = tables.All(t => !string.IsNullOrWhiteSpace(t.Schema));

        foreach (var table in tables)
        {
            var tn = "t" + index;
            if (useSchema)
            {
                var sn = "s" + index;
                conditions.Add($"({schemaColumn} = {Dialect.ParameterPrefix}{sn} AND {tableColumn} = {Dialect.ParameterPrefix}{tn})");
                parameters.Add((sn, table.Schema!.Trim()));
            }
            else
            {
                conditions.Add($"({tableColumn} = {Dialect.ParameterPrefix}{tn})");
            }

            parameters.Add((tn, table.TableName));
            index++;
        }

        return ("(" + string.Join(" OR ", conditions) + ")", parameters);
    }
}

internal static class ReaderExtensions
{
    public static string? GetNullableString(this DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    public static int? GetNullableInt32(this DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
}

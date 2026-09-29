namespace MetaData.Core.Abstractions;

/// <summary>生成好的 SQL 及命名参数（参数名不含前缀符号）。</summary>
public record BuiltSql(string Sql, IReadOnlyDictionary<string, object?> Parameters);

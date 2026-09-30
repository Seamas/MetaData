using System.Text;
using System.Text.RegularExpressions;
using MetaData.Abstractions;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Exceptions;
using MetaData.Providers.Dialects.Building;

namespace MetaData.Providers.Dialects;

/// <summary>方言公共实现：WHERE/ORDER BY 构造、参数化、三种分页模板、连接串通用校验。</summary>
public abstract class DatabaseDialectBase : IDatabaseDialect
{
    public abstract DatabaseType DatabaseType { get; }

    public virtual char ParameterPrefix => '@';

    public abstract string QuoteIdentifier(string name);

    public virtual string QualifyTable(string? schema, string tableName) =>
        string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(tableName)
            : $"{QuoteIdentifier(schema!)}.{QuoteIdentifier(tableName)}";

    public abstract string GetVersionSql();

    public abstract DbVersionInfo ParseVersion(string raw);

    public abstract DataCategory MapDataType(string? dataTypeName, string? fullNativeType, int? numericPrecision, int? numericScale);

    public abstract string BuildConnectionString(ConnectionSettings settings);

    /// <summary>分页语法选择。</summary>
    protected enum PagingSyntax
    {
        /// <summary>LIMIT x OFFSET y（MySQL/PostgreSQL）。</summary>
        LimitOffset,

        /// <summary>ANSI OFFSET .. ROWS FETCH NEXT .. ROWS ONLY（较新版本）。</summary>
        OffsetFetch,

        /// <summary>ROW_NUMBER() 窗口函数包装（老版本回退）。</summary>
        RowNumber
    }

    protected abstract PagingSyntax GetPagingSyntax(DbVersionInfo version);

    /// <summary>Oracle 11g 等老版本在 ROW_NUMBER 方案外再套一层 ROWNUM 以支持下推。</summary>
    protected virtual bool UseRownumWrapper => false;

    public BuiltSql BuildCountQuery(QueryParts parts)
    {
        var ctx = new SqlBuildContext(ParameterPrefix);
        var where = BuildWhere(parts.Filters, ctx);
        var sql = $"SELECT COUNT(*) FROM {QualifyTable(parts.Schema, parts.TableName)}{where}";
        return new BuiltSql(sql, ctx.Parameters);
    }

    public BuiltSql BuildPagedQuery(QueryParts parts, DbVersionInfo version)
    {
        var ctx = new SqlBuildContext(ParameterPrefix);
        var columns = string.Join(", ", parts.SelectColumns.Select(QuoteIdentifier));
        var table = QualifyTable(parts.Schema, parts.TableName);
        var where = BuildWhere(parts.Filters, ctx);
        var sorts = EffectiveSorts(parts);
        var orderBy = BuildOrderBy(sorts);

        switch (GetPagingSyntax(version))
        {
            case PagingSyntax.LimitOffset:
            {
                var limit = ctx.Reserved("__take", parts.Take);
                var offset = ctx.Reserved("__skip", parts.Skip);
                var sql = $"SELECT {columns} FROM {table}{where}{orderBy} LIMIT {limit} OFFSET {offset}";
                return new BuiltSql(sql, ctx.Parameters);
            }

            case PagingSyntax.OffsetFetch:
            {
                if (sorts.Count == 0)
                {
                    throw new MetaDataException("OFFSET/FETCH 分页要求提供排序字段。");
                }

                var offset = ctx.Reserved("__skip", parts.Skip);
                var fetch = ctx.Reserved("__take", parts.Take);
                var sql = $"SELECT {columns} FROM {table}{where}{orderBy} " +
                          $"OFFSET {offset} ROWS FETCH NEXT {fetch} ROWS ONLY";
                return new BuiltSql(sql, ctx.Parameters);
            }

            default:
                return BuildRowNumberPaging(ctx, columns, table, where, orderBy, sorts, parts);
        }
    }

    private BuiltSql BuildRowNumberPaging(
        SqlBuildContext ctx,
        string columns,
        string table,
        string where,
        string orderBy,
        IReadOnlyList<SortItem> sorts,
        QueryParts parts)
    {
        if (sorts.Count == 0)
        {
            throw new MetaDataException("ROW_NUMBER 分页要求提供排序字段。");
        }

        var rn = QuoteIdentifier("__rn");
        var skip = ctx.Reserved("__skip", parts.Skip);
        var end = ctx.Reserved("__end", parts.Skip + parts.Take);

        if (UseRownumWrapper)
        {
            // Oracle 11g：内层 ROWNUM <= end 下推，外层过滤 rn > skip
            var sql = $"SELECT {columns} FROM (" +
                      $"SELECT {QuoteIdentifier("__inner")}.*, ROWNUM AS {rn} FROM (" +
                      $"SELECT {columns} FROM {table}{where}{orderBy}" +
                      $") {QuoteIdentifier("__inner")} WHERE ROWNUM <= {end}" +
                      $") WHERE {rn} > {skip}";
            return new BuiltSql(sql, ctx.Parameters);
        }

        var sql2 = $"SELECT {columns} FROM (" +
                   $"SELECT {columns}, ROW_NUMBER() OVER ({orderBy.Trim()}) AS {rn} " +
                   $"FROM {table}{where}" +
                   $") {QuoteIdentifier("__paged")} WHERE {rn} > {skip} AND {rn} <= {end}";
        return new BuiltSql(sql2, ctx.Parameters);
    }

    private IReadOnlyList<SortItem> EffectiveSorts(QueryParts parts)
    {
        if (parts.Sorts.Count > 0)
        {
            return parts.Sorts;
        }

        // 无显式排序时，用第一列兜底，保证分页结果稳定
        var first = parts.SelectColumns.FirstOrDefault();
        return first is null
            ? []
            : new[] { new SortItem { ColumnName = first, Direction = SortDirection.Asc } };
    }

    protected string BuildOrderBy(IReadOnlyList<SortItem> sorts)
    {
        if (sorts.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(" ORDER BY ");
        for (var i = 0; i < sorts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(QuoteIdentifier(sorts[i].ColumnName));
            sb.Append(sorts[i].Direction == SortDirection.Desc ? " DESC" : " ASC");
        }

        return sb.ToString();
    }

    private string BuildWhere(IReadOnlyList<FilterCondition> filters, SqlBuildContext ctx)
    {
        if (filters.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(" WHERE ");
        for (var i = 0; i < filters.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(" AND ");
            }

            sb.Append(BuildOneFilter(filters[i], ctx));
        }

        return sb.ToString();
    }

    private string BuildOneFilter(FilterCondition filter, SqlBuildContext ctx)
    {
        var col = QuoteIdentifier(filter.ColumnName);
        switch (filter.Operator)
        {
            case FilterOperator.Equal:
                return $"{col} = {ctx.Param(filter.Value)}";
            case FilterOperator.NotEqual:
                return $"{col} <> {ctx.Param(filter.Value)}";
            case FilterOperator.Contains:
                return $"{col} LIKE {ctx.Param("%" + EscapeLike(filter.Value?.ToString()) + "%")} ESCAPE '\\'";
            case FilterOperator.StartsWith:
                return $"{col} LIKE {ctx.Param(EscapeLike(filter.Value?.ToString()) + "%")} ESCAPE '\\'";
            case FilterOperator.EndsWith:
                return $"{col} LIKE {ctx.Param("%" + EscapeLike(filter.Value?.ToString()))} ESCAPE '\\'";
            case FilterOperator.GreaterThan:
                return $"{col} > {ctx.Param(filter.Value)}";
            case FilterOperator.GreaterThanOrEqual:
                return $"{col} >= {ctx.Param(filter.Value)}";
            case FilterOperator.LessThan:
                return $"{col} < {ctx.Param(filter.Value)}";
            case FilterOperator.LessThanOrEqual:
                return $"{col} <= {ctx.Param(filter.Value)}";
            case FilterOperator.Between:
                return $"{col} BETWEEN {ctx.Param(filter.Value)} AND {ctx.Param(filter.Value2)}";
            case FilterOperator.IsNull:
                return $"{col} IS NULL";
            case FilterOperator.IsNotNull:
                return $"{col} IS NOT NULL";
            default:
                throw new MetaDataException($"不支持的查询操作符：{filter.Operator}");
        }
    }

    private static string EscapeLike(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (c is '\\' or '%' or '_')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>从任意版本字符串中解析首个“主.次”版本号。</summary>
    protected static DbVersionInfo ParseFirstVersion(string raw, string? pattern = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DbVersionInfo.Unknown;
        }

        var m = Regex.Match(raw, pattern ?? @"(\d+)\.(\d+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        if (!m.Success)
        {
            var single = Regex.Match(raw, @"(\d+)", RegexOptions.None, TimeSpan.FromSeconds(1));
            return single.Success
                ? new DbVersionInfo { Raw = raw, Major = int.Parse(single.Groups[1].Value) }
                : new DbVersionInfo { Raw = raw };
        }

        return new DbVersionInfo
        {
            Raw = raw,
            Major = int.Parse(m.Groups[1].Value),
            Minor = int.Parse(m.Groups[2].Value)
        };
    }

    protected static string Require(ConnectionSettings s, string value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new MetaDataException($"结构化连接模式下必须填写：{fieldLabel}");
        }

        return value;
    }

    protected static string AppendExtras(string connectionString, string? extraOptions)
    {
        var extra = extraOptions?.Trim().TrimEnd(';');
        return string.IsNullOrWhiteSpace(extra) ? connectionString : connectionString + extra + ";";
    }
}

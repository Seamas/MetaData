using MetaData.Core.Abstractions;
using MetaData.Core.Dialects;
using MetaData.Core.Enums;
using Xunit;

namespace MetaData.Core.Tests;

public class DialectSqlTests
{
    private static QueryParts BuildParts(params FilterCondition[] filters) => new()
    {
        Schema = "dbo",
        TableName = "users",
        SelectColumns = new[] { "Id", "User Name", "Age" },
        Filters = filters,
        Sorts = new[] { new SortItem { ColumnName = "Id" } },
        Skip = 20,
        Take = 10
    };

    private static FilterCondition ContainsFilter => new()
    {
        ColumnName = "User Name",
        Operator = FilterOperator.Contains,
        Value = "a%b_c"
    };

    [Fact]
    public void MySql_Paging_UsesLimitOffset_And_BacktickQuoting()
    {
        var dialect = new MySqlDialect();
        var built = dialect.BuildPagedQuery(BuildParts(ContainsFilter), dialect.ParseVersion("8.0.36"));

        Assert.Contains("LIMIT @__take OFFSET @__skip", built.Sql);
        Assert.Contains("`dbo`.`users`", built.Sql);
        Assert.Contains("`User Name`", built.Sql);
        Assert.Contains("LIKE @f0 ESCAPE '\\'", built.Sql);
        Assert.Equal("%a\\%b\\_c%", built.Parameters["f0"]);
        Assert.Equal(10, built.Parameters["__take"]);
        Assert.Equal(20L, built.Parameters["__skip"]);
    }

    [Fact]
    public void PostgreSql_Paging_UsesLimitOffset_And_DoubleQuoteQuoting()
    {
        var dialect = new PostgreSqlDialect();
        var built = dialect.BuildPagedQuery(BuildParts(), dialect.ParseVersion("PostgreSQL 16.4"));

        Assert.Contains("LIMIT @__take OFFSET @__skip", built.Sql);
        Assert.Contains("\"dbo\".\"users\"", built.Sql);
        Assert.StartsWith("SELECT", built.Sql);
    }

    [Fact]
    public void SqlServer_2012_UsesOffsetFetch()
    {
        var dialect = new SqlServerDialect();
        var built = dialect.BuildPagedQuery(BuildParts(), dialect.ParseVersion("15.0.4223.1"));

        Assert.Contains("OFFSET @__skip ROWS FETCH NEXT @__take ROWS ONLY", built.Sql);
        Assert.Contains("[dbo].[users]", built.Sql);
        Assert.Contains("[Id] ASC", built.Sql);
    }

    [Fact]
    public void SqlServer_2008_FallsBackTo_RowNumber()
    {
        var dialect = new SqlServerDialect();
        var built = dialect.BuildPagedQuery(BuildParts(), dialect.ParseVersion("10.50.6000.34"));

        Assert.True(built.Sql.Contains("ROW_NUMBER() OVER (ORDER BY [Id] ASC)"), "SQL=[" + built.Sql + "]");
        Assert.Contains("[__rn] > @__skip", built.Sql);
        Assert.Contains("[__rn] <= @__end", built.Sql);
        Assert.Equal(30L, built.Parameters["__end"]);
        Assert.DoesNotContain("FETCH NEXT", built.Sql);
    }

    [Fact]
    public void Oracle_19c_UsesOffsetFetch()
    {
        var dialect = new OracleDialect();
        var version = dialect.ParseVersion("Oracle Database 19c Enterprise Edition Release 19.0.0.0.0 - Production");
        Assert.Equal(19, version.Major);

        var built = dialect.BuildPagedQuery(BuildParts(), version);
        Assert.Contains("OFFSET :__skip ROWS FETCH NEXT :__take ROWS ONLY", built.Sql);
        Assert.Contains("\"dbo\".\"users\"", built.Sql);
    }

    [Fact]
    public void Oracle_11g_FallsBackTo_RownumWrapper()
    {
        var dialect = new OracleDialect();
        var version = dialect.ParseVersion("Oracle Database 11g Release 11.2.0.4.0 - 64bit Production");
        Assert.Equal(11, version.Major);

        var built = dialect.BuildPagedQuery(BuildParts(), version);
        Assert.Contains("ROWNUM AS \"__rn\"", built.Sql);
        Assert.Contains("ROWNUM <= :__end", built.Sql);
        Assert.Contains("\"__rn\" > :__skip", built.Sql);
        Assert.DoesNotContain("FETCH NEXT", built.Sql);
    }

    [Fact]
    public void Db2_10_5_UsesOffsetFetch()
    {
        var dialect = new Db2Dialect();
        var built = dialect.BuildPagedQuery(BuildParts(), dialect.ParseVersion("DB2 v11.5.8.0"));

        Assert.Contains("OFFSET @__skip ROWS FETCH NEXT @__take ROWS ONLY", built.Sql);
        Assert.Equal(11, dialect.ParseVersion("DB2 v11.5.8.0").Major);
    }

    [Fact]
    public void Db2_9_7_FallsBackTo_RowNumber()
    {
        var dialect = new Db2Dialect();
        var built = dialect.BuildPagedQuery(BuildParts(), dialect.ParseVersion("DB2 v9.7.0.10"));

        Assert.Contains("ROW_NUMBER() OVER", built.Sql);
        Assert.DoesNotContain("FETCH NEXT", built.Sql);
    }

    [Fact]
    public void CountQuery_BuildsParameterizedWhere()
    {
        var dialect = new MySqlDialect();
        var built = dialect.BuildCountQuery(BuildParts(
            new FilterCondition { ColumnName = "Age", Operator = FilterOperator.GreaterThanOrEqual, Value = 18L },
            new FilterCondition { ColumnName = "Id", Operator = FilterOperator.IsNotNull }));

        Assert.Equal("SELECT COUNT(*) FROM `dbo`.`users` WHERE `Age` >= @f0 AND `Id` IS NOT NULL", built.Sql);
        Assert.Equal(18L, built.Parameters["f0"]);
    }

    [Fact]
    public void BetweenFilter_UsesTwoParameters()
    {
        var dialect = new PostgreSqlDialect();
        var built = dialect.BuildCountQuery(BuildParts(
            new FilterCondition { ColumnName = "Age", Operator = FilterOperator.Between, Value = 1L, Value2 = 10L }));

        Assert.Contains("BETWEEN @f0 AND @f1", built.Sql);
        Assert.Equal(1L, built.Parameters["f0"]);
        Assert.Equal(10L, built.Parameters["f1"]);
    }
}

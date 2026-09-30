using MetaData.Abstractions;
using MetaData.Providers.Dialects;
using MetaData.Abstractions.Enums;
using Xunit;

namespace MetaData.Core.Tests;

public class DialectConnectionAndTypeTests
{
    private static ConnectionSettings SimpleSettings(AuthMode authMode = AuthMode.Basic) => new()
    {
        InputMode = ConnectionInputMode.Simple,
        Host = "db.example.com",
        Port = 3306,
        DatabaseName = "demo",
        UserName = "appuser",
        Password = "p@ss;word",
        AuthMode = authMode
    };

    [Fact]
    public void MySql_BuildConnectionString()
    {
        var cs = new MySqlDialect().BuildConnectionString(SimpleSettings());
        Assert.Equal("Server=db.example.com;Port=3306;Database=demo;Uid=appuser;Pwd=p@ss;word;", cs);
    }

    [Fact]
    public void PostgreSql_BuildConnectionString()
    {
        var cs = new PostgreSqlDialect().BuildConnectionString(SimpleSettings());
        Assert.Equal("Host=db.example.com;Port=3306;Database=demo;Username=appuser;Password=p@ss;word;", cs);
    }

    [Fact]
    public void SqlServer_BuildConnectionString_WithInstanceAndPort()
    {
        var settings = SimpleSettings();
        settings.Port = 1433;
        settings.InstanceName = "SQLEXPRESS";
        var cs = new SqlServerDialect().BuildConnectionString(settings);
        Assert.Equal("Server=db.example.com\\SQLEXPRESS,1433;Database=demo;User Id=appuser;Password=p@ss;word;", cs);
    }

    [Fact]
    public void SqlServer_IntegratedAuth_OmitsCredentials()
    {
        var settings = SimpleSettings(AuthMode.Integrated);
        var cs = new SqlServerDialect().BuildConnectionString(settings);
        Assert.Contains("Integrated Security=True;", cs);
        Assert.DoesNotContain("Password=", cs);
    }

    [Fact]
    public void Db2_BuildConnectionString_DefaultPort()
    {
        var settings = SimpleSettings();
        settings.Port = null;
        var cs = new Db2Dialect().BuildConnectionString(settings);
        Assert.Equal("Server=db.example.com:50000;Database=demo;UID=appuser;PWD=p@ss;word;", cs);
    }

    [Fact]
    public void Oracle_ServiceName_UsesEzConnect()
    {
        var settings = SimpleSettings();
        settings.Port = 1521;
        settings.OracleTargetType = OracleTargetType.ServiceName;
        var cs = new OracleDialect().BuildConnectionString(settings);
        Assert.Equal("User Id=appuser;Password=p@ss;word;Data Source=db.example.com:1521/demo;", cs);
    }

    [Fact]
    public void Oracle_Sid_UsesDescriptor()
    {
        var settings = SimpleSettings();
        settings.Port = 1521;
        settings.OracleTargetType = OracleTargetType.Sid;
        var cs = new OracleDialect().BuildConnectionString(settings);
        Assert.Contains("(CONNECT_DATA=(SID=demo))", cs);
        Assert.Contains("HOST=db.example.com", cs);
    }

    [Fact]
    public void AdvancedMode_ReturnsRawString()
    {
        var settings = new ConnectionSettings
        {
            InputMode = ConnectionInputMode.Advanced,
            AdvancedConnectionString = "custom=value;foo=bar;"
        };
        Assert.Equal("custom=value;foo=bar;", new OracleDialect().BuildConnectionString(settings));
    }

    [Fact]
    public void ExtraOptions_AreAppended()
    {
        var settings = SimpleSettings();
        settings.ExtraOptions = "SslMode=Required;";
        var cs = new MySqlDialect().BuildConnectionString(settings);
        Assert.EndsWith("SslMode=Required;", cs);
    }

    [Theory]
    [InlineData("tinyint", "tinyint(1)", null, null, DataCategory.Boolean)]
    [InlineData("bigint", "bigint", null, null, DataCategory.Number)]
    [InlineData("varchar", "varchar(100)", null, null, DataCategory.String)]
    [InlineData("datetime", "datetime", null, null, DataCategory.DateTime)]
    [InlineData("blob", "blob", null, null, DataCategory.Binary)]
    public void MySql_TypeMapping(string type, string fullType, int? p, int? s, DataCategory expected)
        => Assert.Equal(expected, new MySqlDialect().MapDataType(type, fullType, p, s));

    [Theory]
    [InlineData("integer", DataCategory.Number)]
    [InlineData("boolean", DataCategory.Boolean)]
    [InlineData("timestamp with time zone", DataCategory.DateTime)]
    [InlineData("uuid", DataCategory.Guid)]
    [InlineData("bytea", DataCategory.Binary)]
    [InlineData("character varying", DataCategory.String)]
    public void PostgreSql_TypeMapping(string type, DataCategory expected)
        => Assert.Equal(expected, new PostgreSqlDialect().MapDataType(type, type, null, null));

    [Theory]
    [InlineData("bit", DataCategory.Boolean)]
    [InlineData("int", DataCategory.Number)]
    [InlineData("datetime2", DataCategory.DateTime)]
    [InlineData("uniqueidentifier", DataCategory.Guid)]
    [InlineData("varbinary", DataCategory.Binary)]
    [InlineData("nvarchar", DataCategory.String)]
    public void SqlServer_TypeMapping(string type, DataCategory expected)
        => Assert.Equal(expected, new SqlServerDialect().MapDataType(type, type, null, null));

    [Theory]
    [InlineData("NUMBER", 10, 2, DataCategory.Number)]
    [InlineData("VARCHAR2", 100, null, DataCategory.String)]
    [InlineData("TIMESTAMP(6)", null, null, DataCategory.DateTime)]
    [InlineData("RAW", 16, null, DataCategory.Binary)]
    public void Oracle_TypeMapping(string type, int? p, int? s, DataCategory expected)
        => Assert.Equal(expected, new OracleDialect().MapDataType(type, type, p, s));

    [Theory]
    [InlineData("DECIMAL", DataCategory.Number)]
    [InlineData("VARCHAR", DataCategory.String)]
    [InlineData("TIMESTAMP", DataCategory.DateTime)]
    [InlineData("BLOB", DataCategory.Binary)]
    [InlineData("BOOLEAN", DataCategory.Boolean)]
    public void Db2_TypeMapping(string type, DataCategory expected)
        => Assert.Equal(expected, new Db2Dialect().MapDataType(type, type, null, null));

    [Fact]
    public void UnknownVersion_DoesNotThrow_AndCanStillParse()
    {
        var version = new OracleDialect().ParseVersion("garbage");
        Assert.Equal(0, version.Major);
    }
}

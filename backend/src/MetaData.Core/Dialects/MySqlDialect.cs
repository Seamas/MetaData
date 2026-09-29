using MetaData.Core.Abstractions;
using MetaData.Core.Enums;

namespace MetaData.Core.Dialects;

/// <summary>MySQL 方言（支持 5.7/8.0；MySqlConnector 驱动，参数前缀 @，分页 LIMIT/OFFSET）。</summary>
public class MySqlDialect : DatabaseDialectBase
{
    public override DatabaseType DatabaseType => DatabaseType.MySql;

    public override char ParameterPrefix => '@';

    public override string QuoteIdentifier(string name)
        => "`" + name.Replace("`", "``") + "`";

    public override string GetVersionSql() => "SELECT VERSION()";

    public override DbVersionInfo ParseVersion(string raw) => ParseFirstVersion(raw);

    protected override PagingSyntax GetPagingSyntax(DbVersionInfo version) => PagingSyntax.LimitOffset;

    public override DataCategory MapDataType(string? dataTypeName, string? fullNativeType, int? numericPrecision, int? numericScale)
    {
        var t = (dataTypeName ?? string.Empty).ToLowerInvariant();
        var full = (fullNativeType ?? string.Empty).ToLowerInvariant();

        // MySQL 惯例：tinyint(1) 用作布尔
        if (t == "bit" || t == "tinyint" && full.StartsWith("tinyint(1)"))
        {
            return DataCategory.Boolean;
        }

        return t switch
        {
            "tinyint" or "smallint" or "mediumint" or "int" or "integer" or "bigint"
                or "float" or "double" or "decimal" or "numeric" or "year" => DataCategory.Number,
            "date" or "datetime" or "timestamp" or "time" => DataCategory.DateTime,
            "binary" or "varbinary" or "tinyblob" or "blob" or "mediumblob" or "longblob" => DataCategory.Binary,
            _ => DataCategory.String
        };
    }

    public override string BuildConnectionString(ConnectionSettings s)
    {
        if (s.InputMode == ConnectionInputMode.Advanced)
        {
            return Require(s, s.AdvancedConnectionString!, "高级连接串");
        }

        var host = Require(s, s.Host!, "主机地址");
        var db = Require(s, s.DatabaseName!, "数据库名");
        var user = Require(s, s.UserName!, "用户名");

        var cs = $"Server={host};";
        if (s.Port is > 0)
        {
            cs += $"Port={s.Port};";
        }

        cs += $"Database={db};Uid={user};Pwd={s.Password ?? string.Empty};";
        return AppendExtras(cs, s.ExtraOptions);
    }
}

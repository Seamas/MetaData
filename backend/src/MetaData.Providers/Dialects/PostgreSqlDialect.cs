using MetaData.Abstractions;
using MetaData.Abstractions.Enums;

namespace MetaData.Providers.Dialects;

/// <summary>PostgreSQL 方言（支持 12+；Npgsql 驱动，参数前缀 @，分页 LIMIT/OFFSET）。</summary>
public class PostgreSqlDialect : DatabaseDialectBase
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSql;

    public override char ParameterPrefix => '@';

    public override string QuoteIdentifier(string name)
        => "\"" + name.Replace("\"", "\"\"") + "\"";

    public override string GetVersionSql() => "SELECT version()";

    public override DbVersionInfo ParseVersion(string raw) => ParseFirstVersion(raw);

    protected override PagingSyntax GetPagingSyntax(DbVersionInfo version) => PagingSyntax.LimitOffset;

    public override DataCategory MapDataType(string? dataTypeName, string? fullNativeType, int? numericPrecision, int? numericScale)
    {
        var t = (dataTypeName ?? string.Empty).ToLowerInvariant();

        return t switch
        {
            "integer" or "bigint" or "smallint" or "numeric" or "decimal"
                or "real" or "double precision" or "smallserial" or "serial" or "bigserial" or "money" => DataCategory.Number,
            "boolean" or "bool" => DataCategory.Boolean,
            "date" or "time" or "time with time zone" or "time without time zone"
                or "timestamp" or "timestamp with time zone" or "timestamp without time zone" => DataCategory.DateTime,
            "uuid" => DataCategory.Guid,
            "bytea" => DataCategory.Binary,
            "character" or "character varying" or "text" or "json" or "jsonb" or "xml"
                or "interval" or "char" or "varchar" or "citext" or "name" => DataCategory.String,
            _ => DataCategory.Unknown
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

        var cs = $"Host={host};";
        if (s.Port is > 0)
        {
            cs += $"Port={s.Port};";
        }

        cs += $"Database={db};Username={user};Password={s.Password ?? string.Empty};";
        return AppendExtras(cs, s.ExtraOptions);
    }
}

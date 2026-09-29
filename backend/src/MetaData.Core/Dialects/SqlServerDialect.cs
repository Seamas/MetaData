using MetaData.Core.Abstractions;
using MetaData.Core.Enums;

namespace MetaData.Core.Dialects;

/// <summary>SQL Server 方言（支持 2012+；Microsoft.Data.SqlClient，参数前缀 @；
/// 2012(主版本11)起 OFFSET/FETCH，更早回退 ROW_NUMBER）。</summary>
public class SqlServerDialect : DatabaseDialectBase
{
    public override DatabaseType DatabaseType => DatabaseType.SqlServer;

    public override char ParameterPrefix => '@';

    public override string QuoteIdentifier(string name)
        => "[" + name.Replace("]", "]]") + "]";

    public override string GetVersionSql()
        => "SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(200))";

    public override DbVersionInfo ParseVersion(string raw) => ParseFirstVersion(raw);

    protected override PagingSyntax GetPagingSyntax(DbVersionInfo version)
        => version.AtLeast(11) ? PagingSyntax.OffsetFetch : PagingSyntax.RowNumber;

    public override DataCategory MapDataType(string? dataTypeName, string? fullNativeType, int? numericPrecision, int? numericScale)
    {
        var t = (dataTypeName ?? string.Empty).ToLowerInvariant();

        return t switch
        {
            "bit" => DataCategory.Boolean,
            "tinyint" or "smallint" or "int" or "bigint" or "decimal" or "numeric"
                or "money" or "smallmoney" or "float" or "real" => DataCategory.Number,
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" or "time" => DataCategory.DateTime,
            "uniqueidentifier" => DataCategory.Guid,
            "binary" or "varbinary" or "image" or "timestamp" => DataCategory.Binary,
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

        var server = host;
        if (!string.IsNullOrWhiteSpace(s.InstanceName))
        {
            server += "\\" + s.InstanceName.Trim();
        }

        if (s.Port is > 0)
        {
            server += "," + s.Port;
        }

        var cs = $"Server={server};Database={db};";

        if (s.AuthMode == AuthMode.Integrated)
        {
            cs += "Integrated Security=True;";
        }
        else
        {
            var user = Require(s, s.UserName!, "用户名");
            cs += $"User Id={user};Password={s.Password ?? string.Empty};";
        }

        return AppendExtras(cs, s.ExtraOptions);
    }
}

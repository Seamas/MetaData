using MetaData.Core.Abstractions;
using MetaData.Core.Enums;

namespace MetaData.Core.Dialects;

/// <summary>DB2 for LUW 方言（支持 10.1+；Net.IBM.Data.Db2 驱动，参数前缀 @；
/// 10.1 起 OFFSET/FETCH，更早回退 ROW_NUMBER）。</summary>
public class Db2Dialect : DatabaseDialectBase
{
    public override DatabaseType DatabaseType => DatabaseType.Db2;

    public override char ParameterPrefix => '@';

    public override string QuoteIdentifier(string name)
        => "\"" + name.Replace("\"", "\"\"") + "\"";

    public override string GetVersionSql()
        => "SELECT SERVICE_LEVEL FROM SYSIBMADM.ENV_INST_INFO";

    public override DbVersionInfo ParseVersion(string raw)
        => ParseFirstVersion(raw, @"(\d+)\.(\d+)");

    protected override PagingSyntax GetPagingSyntax(DbVersionInfo version)
        // 10.1 开始支持 OFFSET/FETCH
        => version.AtLeast(10, 1) ? PagingSyntax.OffsetFetch : PagingSyntax.RowNumber;

    public override DataCategory MapDataType(string? dataTypeName, string? fullNativeType, int? numericPrecision, int? numericScale)
    {
        var t = (dataTypeName ?? string.Empty).ToUpperInvariant();

        return t switch
        {
            "BOOLEAN" => DataCategory.Boolean,
            "SMALLINT" or "INTEGER" or "INT" or "BIGINT" or "DECIMAL" or "DEC"
                or "NUMERIC" or "REAL" or "DOUBLE" or "DECFLOAT" or "FLOAT" => DataCategory.Number,
            "DATE" or "TIME" or "TIMESTAMP" => DataCategory.DateTime,
            "BLOB" or "BINARY" or "VARBINARY" => DataCategory.Binary,
            "CHAR" or "CHARACTER" or "VARCHAR" or "LONG VARCHAR" or "CLOB"
                or "GRAPHIC" or "VARGRAPHIC" or "LONG VARGRAPHIC" or "DBCLOB" or "XML" => DataCategory.String,
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
        var port = s.Port is > 0 ? s.Port.Value : 50000;

        var cs = $"Server={host}:{port};Database={db};UID={user};PWD={s.Password ?? string.Empty};";
        return AppendExtras(cs, s.ExtraOptions);
    }
}

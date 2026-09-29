using System.Text.RegularExpressions;
using MetaData.Core.Abstractions;
using MetaData.Core.Enums;

namespace MetaData.Core.Dialects;

/// <summary>Oracle 方言（支持 11gR2+；Oracle.ManagedDataAccess 驱动，参数前缀 :；
/// 12c 起 OFFSET/FETCH，11g 回退 ROWNUM 包装；连接目标区分 ServiceName/SID）。</summary>
public class OracleDialect : DatabaseDialectBase
{
    public override DatabaseType DatabaseType => DatabaseType.Oracle;

    public override char ParameterPrefix => ':';

    public override string QuoteIdentifier(string name)
        => "\"" + name.Replace("\"", "\"\"") + "\"";

    public override string GetVersionSql()
        => "SELECT BANNER FROM V$VERSION WHERE ROWNUM = 1";

    public override DbVersionInfo ParseVersion(string raw)
    {
        raw ??= string.Empty;
        // 形如：Oracle Database 19c Enterprise Edition Release 19.0.0.0.0 - Production
        var m = Regex.Match(raw, @"Release\s+(\d+)\.(\d+)", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        if (m.Success)
        {
            return new DbVersionInfo
            {
                Raw = raw,
                Major = int.Parse(m.Groups[1].Value),
                Minor = int.Parse(m.Groups[2].Value)
            };
        }

        return ParseFirstVersion(raw);
    }

    protected override PagingSyntax GetPagingSyntax(DbVersionInfo version)
        => version.AtLeast(12) ? PagingSyntax.OffsetFetch : PagingSyntax.RowNumber;

    protected override bool UseRownumWrapper => true;

    public override DataCategory MapDataType(string? dataTypeName, string? fullNativeType, int? numericPrecision, int? numericScale)
    {
        // Oracle 的 DATA_TYPE 可能形如 TIMESTAMP(6)、INTERVAL YEAR(2) TO MONTH，先去括号后缀
        var t = (dataTypeName ?? string.Empty).ToUpperInvariant();
        var paren = t.IndexOf('(');
        if (paren >= 0)
        {
            t = t[..paren].Trim();
        }

        switch (t)
        {
            case "NUMBER":
            case "FLOAT":
            case "BINARY_FLOAT":
            case "BINARY_DOUBLE":
            case "INTEGER":
            case "INT":
            case "SMALLINT":
            case "BINARY_INTEGER":
            case "PLS_INTEGER":
                return DataCategory.Number;

            case "DATE":
            case "TIMESTAMP":
            case "TIMESTAMP WITH TIME ZONE":
            case "TIMESTAMP WITH LOCAL TIME ZONE":
                return DataCategory.DateTime;

            case "BOOLEAN":
                return DataCategory.Boolean;

            case "RAW":
            case "LONG RAW":
            case "BLOB":
            case "BFILE":
                return DataCategory.Binary;

            case "VARCHAR2":
            case "NVARCHAR2":
            case "CHAR":
            case "NCHAR":
            case "CLOB":
            case "NCLOB":
            case "LONG":
            case "ROWID":
            case "UROWID":
            case "XMLTYPE":
                return DataCategory.String;

            default:
                return DataCategory.Unknown;
        }
    }

    public override string BuildConnectionString(ConnectionSettings s)
    {
        if (s.InputMode == ConnectionInputMode.Advanced)
        {
            return Require(s, s.AdvancedConnectionString!, "高级连接串");
        }

        var host = Require(s, s.Host!, "主机地址");
        var serviceOrSid = Require(s, s.DatabaseName!, "服务名或 SID");
        var user = Require(s, s.UserName!, "用户名");
        var port = s.Port is > 0 ? s.Port.Value : 1521;
        var pwd = s.Password ?? string.Empty;

        string dataSource;
        if (s.OracleTargetType == OracleTargetType.Sid)
        {
            dataSource = $"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port}))(CONNECT_DATA=(SID={serviceOrSid})))";
        }
        else
        {
            dataSource = $"{host}:{port}/{serviceOrSid}";
        }

        var cs = $"User Id={user};Password={pwd};Data Source={dataSource};";
        return AppendExtras(cs, s.ExtraOptions);
    }
}

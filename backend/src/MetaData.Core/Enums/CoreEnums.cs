namespace MetaData.Core.Enums;

/// <summary>支持的业务数据库类型。</summary>
public enum DatabaseType
{
    MySql = 1,
    PostgreSql = 2,
    SqlServer = 3,
    Db2 = 4,
    Oracle = 5
}

/// <summary>连接信息录入方式：结构化表单 / 高级原始连接串。</summary>
public enum ConnectionInputMode
{
    Simple = 1,
    Advanced = 2
}

/// <summary>认证方式（集成认证主要用于 SQL Server Windows 身份验证）。</summary>
public enum AuthMode
{
    Basic = 1,
    Integrated = 2
}

/// <summary>Oracle 连接目标类型：服务名 / SID。</summary>
public enum OracleTargetType
{
    ServiceName = 1,
    Sid = 2
}

/// <summary>字段数据归一化分类，决定前端控件与查询操作符。</summary>
public enum DataCategory
{
    Unknown = 0,
    String = 1,
    Number = 2,
    DateTime = 3,
    Boolean = 4,
    Guid = 5,
    Binary = 6
}

/// <summary>过滤条件操作符。</summary>
public enum FilterOperator
{
    Equal = 1,
    NotEqual = 2,
    Contains = 3,
    StartsWith = 4,
    EndsWith = 5,
    GreaterThan = 6,
    GreaterThanOrEqual = 7,
    LessThan = 8,
    LessThanOrEqual = 9,
    Between = 10,
    IsNull = 11,
    IsNotNull = 12
}

/// <summary>排序方向。</summary>
public enum SortDirection
{
    Asc = 1,
    Desc = 2
}

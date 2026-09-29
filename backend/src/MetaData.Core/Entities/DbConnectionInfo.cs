using MetaData.Core.Enums;

namespace MetaData.Core.Entities;

/// <summary>数据库连接元数据。连接串不直接落库，由方言按结构化字段实时拼装；密码/高级串密文存储。</summary>
public class DbConnectionInfo
{
    public long Id { get; set; }

    /// <summary>连接名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>数据库类型。</summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>录入方式：结构化 / 高级原始串。</summary>
    public ConnectionInputMode InputMode { get; set; } = ConnectionInputMode.Simple;

    #region Simple 模式字段

    public string? Host { get; set; }

    public int? Port { get; set; }

    /// <summary>MySQL 库名 / PostgreSQL 数据库 / SQL Server 库 / DB2 数据库 / Oracle 服务名或 SID。</summary>
    public string? DatabaseName { get; set; }

    public string? UserName { get; set; }

    /// <summary>密码密文（DataProtection 保护，前缀 protected:）。</summary>
    public string? PasswordProtected { get; set; }

    /// <summary>认证方式。</summary>
    public AuthMode AuthMode { get; set; } = AuthMode.Basic;

    /// <summary>SQL Server 实例名（可选）。</summary>
    public string? InstanceName { get; set; }

    /// <summary>Oracle 连接目标：服务名 / SID。</summary>
    public OracleTargetType? OracleTargetType { get; set; }

    /// <summary>附加连接参数原文（按各驱动 key=value 语法追加）。</summary>
    public string? ExtraOptions { get; set; }

    #endregion

    #region Advanced 模式字段

    /// <summary>高级模式下的完整连接串密文。</summary>
    public string? AdvancedConnectionStringProtected { get; set; }

    #endregion

    /// <summary>连接成功后探测并缓存的数据库版本原文。</summary>
    public string? ServerVersion { get; set; }

    /// <summary>默认 Schema/Owner（如 public、dbo、Oracle 用户名）。</summary>
    public string? DefaultSchema { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<TableMetadata> Tables { get; set; } = new List<TableMetadata>();
}

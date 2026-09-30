using MetaData.Abstractions.Enums;

namespace MetaData.Core.Models;

/// <summary>数据库连接 DTO。密码/高级连接串仅接收明文用于保存测试，任何输出场景均为 null。</summary>
public class ConnectionDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DatabaseType DatabaseType { get; set; }

    public ConnectionInputMode InputMode { get; set; } = ConnectionInputMode.Simple;

    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? DatabaseName { get; set; }

    public string? UserName { get; set; }

    /// <summary>明文密码（仅入参）；编辑时留空表示不修改。</summary>
    public string? Password { get; set; }

    public AuthMode AuthMode { get; set; } = AuthMode.Basic;

    public string? InstanceName { get; set; }

    public OracleTargetType? OracleTargetType { get; set; }

    public string? ExtraOptions { get; set; }

    /// <summary>高级模式完整连接串（仅入参）。</summary>
    public string? AdvancedConnectionString { get; set; }

    public string? ServerVersion { get; set; }

    public string? DefaultSchema { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Remark { get; set; }

    /// <summary>是否已保存密码（出参）。</summary>
    public bool HasPassword { get; set; }

    /// <summary>是否已保存高级连接串（出参）。</summary>
    public bool HasAdvancedConnectionString { get; set; }
}

using MetaData.Abstractions.Enums;

namespace MetaData.Abstractions;

/// <summary>解密后的连接设置（明文仅在内存中传递，不落地、不序列化到前端）。</summary>
public class ConnectionSettings
{
    public ConnectionInputMode InputMode { get; set; } = ConnectionInputMode.Simple;

    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? DatabaseName { get; set; }

    public string? UserName { get; set; }

    /// <summary>明文密码。</summary>
    public string? Password { get; set; }

    public AuthMode AuthMode { get; set; } = AuthMode.Basic;

    public string? InstanceName { get; set; }

    public OracleTargetType? OracleTargetType { get; set; }

    public string? ExtraOptions { get; set; }

    /// <summary>Advanced 模式下解密后的完整连接串。</summary>
    public string? AdvancedConnectionString { get; set; }
}

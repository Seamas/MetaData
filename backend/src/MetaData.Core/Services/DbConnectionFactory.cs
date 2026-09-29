using System.Data.Common;
using MetaData.Core.Abstractions;
using MetaData.Core.Dialects;
using MetaData.Core.Entities;

namespace MetaData.Core.Services;

/// <summary>默认连接工厂：解析驱动与方言 → 解密 → 拼串 → 打开连接。</summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IDbProviderRegistry _providers;
    private readonly IDialectRegistry _dialects;
    private readonly ISecretProtector _protector;

    public DbConnectionFactory(IDbProviderRegistry providers, IDialectRegistry dialects, ISecretProtector protector)
    {
        _providers = providers;
        _dialects = dialects;
        _protector = protector;
    }

    public async Task<DbConnection> OpenAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default)
    {
        var factory = _providers.Resolve(connection.DatabaseType);
        var dialect = _dialects.Resolve(connection.DatabaseType);

        var settings = new ConnectionSettings
        {
            InputMode = connection.InputMode,
            Host = connection.Host,
            Port = connection.Port,
            DatabaseName = connection.DatabaseName,
            UserName = connection.UserName,
            Password = _protector.Unprotect(connection.PasswordProtected),
            AuthMode = connection.AuthMode,
            InstanceName = connection.InstanceName,
            OracleTargetType = connection.OracleTargetType,
            ExtraOptions = connection.ExtraOptions,
            AdvancedConnectionString = _protector.Unprotect(connection.AdvancedConnectionStringProtected)
        };

        var connectionString = dialect.BuildConnectionString(settings);

        var conn = factory.CreateConnection()
                   ?? throw new InvalidOperationException($"驱动工厂 {factory.GetType().Name} 未能创建连接。");
        conn.ConnectionString = connectionString;
        await conn.OpenAsync(cancellationToken);
        return conn;
    }
}

using System.Data.Common;
using MetaData.Abstractions;
using MetaData.Core.Abstractions.Services;
using MetaData.Core.Entities;

namespace MetaData.Infrastructure.Data;

/// <summary>默认连接工厂：解析驱动与方言 → 解密 → 拼串 → 打开连接。</summary>
internal sealed class DbConnectionFactory(
    IDbProviderRegistry providers,
    IDialectRegistry dialects,
    ISecretProtector protector) : IDbConnectionFactory
{
    public async Task<DbConnection> OpenAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default)
    {
        var factory = providers.Resolve(connection.DatabaseType);
        var dialect = dialects.Resolve(connection.DatabaseType);

        var settings = new ConnectionSettings
        {
            InputMode = connection.InputMode,
            Host = connection.Host,
            Port = connection.Port,
            DatabaseName = connection.DatabaseName,
            UserName = connection.UserName,
            Password = protector.Unprotect(connection.PasswordProtected),
            AuthMode = connection.AuthMode,
            InstanceName = connection.InstanceName,
            OracleTargetType = connection.OracleTargetType,
            ExtraOptions = connection.ExtraOptions,
            AdvancedConnectionString = protector.Unprotect(connection.AdvancedConnectionStringProtected)
        };

        var connectionString = dialect.BuildConnectionString(settings);

        var conn = factory.CreateConnection()
                   ?? throw new InvalidOperationException($"驱动工厂 {factory.GetType().Name} 未能创建连接。");
        conn.ConnectionString = connectionString;
        await conn.OpenAsync(cancellationToken);
        return conn;
    }
}

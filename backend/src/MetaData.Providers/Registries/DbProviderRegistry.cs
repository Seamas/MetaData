using System.Collections.Concurrent;
using System.Data.Common;
using MetaData.Abstractions;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Exceptions;

namespace MetaData.Providers.Registries;

/// <summary>ADO.NET DbProviderFactory 注册表（宿主启动时注册具体驱动工厂）。</summary>
public class DbProviderRegistry : IDbProviderRegistry
{
    private static readonly Dictionary<DatabaseType, string> PackageHints = new()
    {
        [DatabaseType.MySql] = "MySqlConnector",
        [DatabaseType.PostgreSql] = "Npgsql",
        [DatabaseType.SqlServer] = "Microsoft.Data.SqlClient",
        [DatabaseType.Db2] = "Net.IBM.Data.Db2",
        [DatabaseType.Oracle] = "Oracle.ManagedDataAccess"
    };

    private readonly ConcurrentDictionary<DatabaseType, DbProviderFactory> _factories = new();

    public DbProviderRegistry(IEnumerable<DatabaseProviderRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            _factories[registration.DatabaseType] = registration.Factory;
        }
    }

    public void Register(DatabaseType databaseType, DbProviderFactory factory)
    {
        _factories[databaseType] = factory;
    }

    public DbProviderFactory Resolve(DatabaseType databaseType)
    {
        if (_factories.TryGetValue(databaseType, out var factory))
        {
            return factory;
        }

        PackageHints.TryGetValue(databaseType, out var hint);
        throw new DriverNotRegisteredException(databaseType, hint ?? "对应数据库的 ADO.NET 驱动包");
    }

    public bool IsRegistered(DatabaseType databaseType) => _factories.ContainsKey(databaseType);
}

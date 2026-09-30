using System.Data.Common;
using MetaData.Abstractions;
using MetaData.Providers.Dialects;
using MetaData.Providers.Registries;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Schema;
using MetaData.Providers.SchemaInspection;
using MetaData.Core.Security;
using MetaData.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MetaData.Core;

/// <summary>
/// 注册 MetaData 模块的业务服务、方言与注册表。模块不提供 DbContext：
/// 宿主使用集成 DbContext 实现 <see cref="Data.IMetaDataDbContext"/>，并在 OnModelCreating
/// 中调用 ConfigureMetaData；业务库 ADO.NET 驱动通过 AddDatabaseProvider 按需注册。
/// </summary>
public static class MetaDataServiceCollectionExtensions
{
    public static IServiceCollection AddMetaDataCore(
        this IServiceCollection services,
        Action<MetaDataOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // 安全与用户身份
        services.AddDataProtection();
        services.TryAddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.TryAddSingleton<ICurrentUser, DefaultCurrentUser>();

        // ADO.NET 驱动注册表
        services.AddSingleton<IDbProviderRegistry, DbProviderRegistry>();

        // 5 种方言（单例、无驱动依赖）
        IDatabaseDialect[] dialects =
        [
            new MySqlDialect(),
            new PostgreSqlDialect(),
            new SqlServerDialect(),
            new Db2Dialect(),
            new OracleDialect()
        ];
        foreach (var dialect in dialects)
        {
            services.AddSingleton(dialect);
        }

        services.AddSingleton<IEnumerable<IDatabaseDialect>>(dialects);
        services.AddSingleton<IDialectRegistry, DialectRegistry>();

        // 5 种结构读取器
        services.AddSingleton<ISchemaInspector>(new MySqlSchemaInspector(dialects.OfType<MySqlDialect>().First()));
        services.AddSingleton<ISchemaInspector>(new PostgreSqlSchemaInspector(dialects.OfType<PostgreSqlDialect>().First()));
        services.AddSingleton<ISchemaInspector>(new SqlServerSchemaInspector(dialects.OfType<SqlServerDialect>().First()));
        services.AddSingleton<ISchemaInspector>(new Db2SchemaInspector(dialects.OfType<Db2Dialect>().First()));
        services.AddSingleton<ISchemaInspector>(new OracleSchemaInspector(dialects.OfType<OracleDialect>().First()));
        services.AddSingleton<ISchemaInspectorRegistry, SchemaInspectorRegistry>();

        // 业务服务
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<ConnectionService>();
        services.AddScoped<MetadataService>();
        services.AddScoped<MetadataImportService>();
        services.AddScoped<DataQueryService>();
        services.AddScoped<UserPreferenceService>();

        return services;
    }

    /// <summary>注册业务数据库的 ADO.NET 驱动工厂，例如 AddDatabaseProvider(DatabaseType.MySql, MySqlConnectorFactory.Instance)。</summary>
    public static IServiceCollection AddDatabaseProvider(
        this IServiceCollection services,
        DatabaseType databaseType,
        DbProviderFactory factory)
        => services.AddSingleton(new DatabaseProviderRegistration(databaseType, factory));
}

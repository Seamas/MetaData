using System.Data.Common;
using MetaData.Abstractions;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Schema;
using MetaData.Core.Repositories;
using MetaData.Infrastructure.Data;
using MetaData.Infrastructure.Repositories;
using MetaData.Infrastructure.Security;
using MetaData.Infrastructure.UnitOfWork;
using MetaData.Providers.Dialects;
using MetaData.Providers.Registries;
using MetaData.Providers.SchemaInspection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wang.Seamas.Shared.UnitOfWork;

namespace MetaData.Infrastructure;

/// <summary>
/// 注册 MetaData 模块的基础设施：数据保护、驱动/方言/结构读取器注册表、
/// 仓储与工作单元。模块不提供 DbContext：宿主使用集成 DbContext 实现
/// <see cref="MetaData.Core.Data.IMetaDataDbContext"/>，并在 OnModelCreating 中
/// ApplyConfigurationsFromAssembly 加载本程序集的实体配置；
/// 业务库 ADO.NET 驱动通过 AddDatabaseProvider 按需注册。
/// 应用服务请另外调用 MetaData.Application 的 AddMetaDataApplication。
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddMetaDataInfrastructure(this IServiceCollection services)
    {
        // 安全
        services.AddDataProtection();
        services.TryAddSingleton<ISecretProtector, DataProtectionSecretProtector>();

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

        // 仓储与工作单元
        services.AddScoped<IDbConnectionInfoRepository, DbConnectionInfoRepository>();
        services.AddScoped<ITableMetadataRepository, TableMetadataRepository>();
        services.AddScoped<IFieldMetadataRepository, FieldMetadataRepository>();
        services.AddScoped<IUserFieldPreferenceRepository, UserFieldPreferenceRepository>();
        services.AddScoped<IUnitOfWork, MetaDataUnitOfWork>();

        // ADO.NET 连接工厂
        services.AddScoped<MetaData.Core.Abstractions.Services.IDbConnectionFactory, DbConnectionFactory>();

        return services;
    }

    /// <summary>注册业务数据库的 ADO.NET 驱动工厂，例如 AddDatabaseProvider(DatabaseType.MySql, MySqlConnectorFactory.Instance)。</summary>
    public static IServiceCollection AddDatabaseProvider(
        this IServiceCollection services,
        DatabaseType databaseType,
        DbProviderFactory factory)
        => services.AddSingleton(new DatabaseProviderRegistration(databaseType, factory));
}

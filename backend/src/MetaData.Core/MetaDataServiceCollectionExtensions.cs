using System.Data.Common;
using MetaData.Core.Abstractions;
using MetaData.Core.Data;
using MetaData.Core.Dialects;
using MetaData.Core.Enums;
using MetaData.Core.Infrastructure;
using MetaData.Core.SchemaInspection;
using MetaData.Core.Security;
using MetaData.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MetaData.Core;

/// <summary>
/// 核心模块一键注册：宿主仅需传入元数据库的 EF Core 提供程序配置即可启动。
/// 业务库 ADO.NET 驱动通过 AddDatabaseProvider 按需注册，核心模块不引用任何驱动包。
/// </summary>
public static class MetaDataServiceCollectionExtensions
{
    public const string DevCorsPolicyName = "MetaDataDev";

    public static IServiceCollection AddMetaDataCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureMetadataDb,
        Action<MetaDataOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // 元数据库（具体提供程序由宿主配置）
        services.AddDbContextFactory<MetaDataDbContext>(configureMetadataDb);

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
        services.AddSingleton<SchemaInspectorRegistry>();

        // 业务服务
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<ConnectionService>();
        services.AddScoped<MetadataService>();
        services.AddScoped<MetadataImportService>();
        services.AddScoped<DataQueryService>();
        services.AddScoped<UserPreferenceService>();

        // 核心模块内的 Controller 自动作为应用部件加载，枚举统一按字符串输出
        services.AddControllers()
            .AddApplicationPart(typeof(MetaDataServiceCollectionExtensions).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        // 开发环境便捷 CORS（宿主 UseCors 时启用）
        services.AddCors(options => options.AddPolicy(DevCorsPolicyName, policy =>
            policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()));

        return services;
    }

    /// <summary>注册业务数据库的 ADO.NET 驱动工厂，例如 AddDatabaseProvider(DatabaseType.MySql, MySqlConnectorFactory.Instance)。</summary>
    public static IServiceCollection AddDatabaseProvider(
        this IServiceCollection services,
        DatabaseType databaseType,
        DbProviderFactory factory)
        => services.AddSingleton(new DatabaseProviderRegistration(databaseType, factory));

    /// <summary>启用异常中间件并确保元数据库已创建（首版使用 EnsureCreated）。</summary>
    public static async Task<IApplicationBuilder> UseMetaDataCoreAsync(this IApplicationBuilder app)
    {
        app.UseMiddleware<BusinessExceptionMiddleware>();

        using var scope = app.ApplicationServices.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MetaDataDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();

        return app;
    }
}

using MetaData.Application.Interfaces;
using MetaData.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MetaData.Application;

/// <summary>
/// 注册 MetaData 模块的业务用例服务（面向接口）。
/// 需先调用 MetaData.Infrastructure 的 <c>AddMetaDataInfrastructure</c> 完成仓储/方言/安全组件注册。
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddMetaDataApplication(this IServiceCollection services)
    {
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<IMetadataService, MetadataService>();
        services.AddScoped<IMetadataImportService, MetadataImportService>();
        services.AddScoped<IDataQueryService, DataQueryService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();

        return services;
    }
}

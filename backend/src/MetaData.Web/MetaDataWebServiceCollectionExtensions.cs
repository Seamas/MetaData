using MetaData.Web.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace MetaData.Web;

/// <summary>
/// 注册 MetaData 模块的 HTTP 接口层：加载本程序集的 Controller，枚举统一按字符串输出。
/// 宿主调用 AddMetaDataCore 之后再调用本方法即可获得 /api/* 接口。
/// </summary>
public static class MetaDataWebServiceCollectionExtensions
{
    public static IServiceCollection AddMetaDataWeb(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(ConnectionsController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        return services;
    }
}

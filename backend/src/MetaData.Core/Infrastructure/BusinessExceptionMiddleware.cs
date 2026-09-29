using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MetaData.Core.Infrastructure;

/// <summary>业务异常统一转成 400 JSON；未注册驱动等友好错误也由此返回。</summary>
public class BusinessExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;

    public BusinessExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = ex.Message }, JsonOptions));
        }
    }
}

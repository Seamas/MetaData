using MetaData.Core.Abstractions;
using Microsoft.Extensions.Options;

namespace MetaData.Core.Services;

/// <summary>未接入鉴权前的默认用户：读配置 MetaData:DefaultUserId，默认 default。</summary>
public class DefaultCurrentUser : ICurrentUser
{
    public DefaultCurrentUser(IOptions<MetaDataOptions> options)
    {
        UserId = string.IsNullOrWhiteSpace(options.Value.DefaultUserId) ? "default" : options.Value.DefaultUserId;
    }

    public string UserId { get; }
}

/// <summary>核心模块配置项。</summary>
public class MetaDataOptions
{
    public string? DefaultUserId { get; set; }
}

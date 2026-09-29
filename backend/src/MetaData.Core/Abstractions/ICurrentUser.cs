namespace MetaData.Core.Abstractions;

/// <summary>当前用户抽象。暂不接入鉴权，默认返回固定开发用户；将来替换为权限体系实现即可。</summary>
public interface ICurrentUser
{
    string UserId { get; }
}

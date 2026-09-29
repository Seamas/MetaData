namespace MetaData.Core.Infrastructure;

/// <summary>业务异常：消息可直接展示给前端。</summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}

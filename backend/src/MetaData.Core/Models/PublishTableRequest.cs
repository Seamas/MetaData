namespace MetaData.Core.Models;

/// <summary>表发布/取消发布请求。</summary>
public class PublishTableRequest
{
    public long TableId { get; set; }

    public bool IsPublished { get; set; }
}

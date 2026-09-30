namespace MetaData.Application.Models;

/// <summary>批量保存字段配置请求。</summary>
public class SaveFieldsRequest
{
    public long TableId { get; set; }

    public List<FieldDto> Fields { get; set; } = [];
}

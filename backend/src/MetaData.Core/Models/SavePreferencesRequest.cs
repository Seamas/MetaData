namespace MetaData.Core.Models;

/// <summary>保存用户字段偏好请求。</summary>
public class SavePreferencesRequest
{
    public long TableId { get; set; }

    public List<FieldPreferenceDto> Items { get; set; } = [];
}

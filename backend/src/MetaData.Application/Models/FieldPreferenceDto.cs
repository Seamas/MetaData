namespace MetaData.Application.Models;

/// <summary>用户个性化字段偏好（顺序/显隐/宽度）。</summary>
public class FieldPreferenceDto
{
    public long FieldId { get; set; }

    public int Ordinal { get; set; }

    public bool IsVisible { get; set; } = true;

    public int? Width { get; set; }
}

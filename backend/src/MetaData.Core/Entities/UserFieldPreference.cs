using Wang.Seamas.Shared.Entities;

namespace MetaData.Core.Entities;

/// <summary>用户级字段个性化配置（列顺序、显隐、宽度），仅对该用户生效。</summary>
public class UserFieldPreference : BaseEntity<long>
{
    public string UserId { get; set; } = string.Empty;

    public long TableId { get; set; }

    public long FieldId { get; set; }

    public int Ordinal { get; set; }

    public bool IsVisible { get; set; } = true;

    public int? Width { get; set; }
}

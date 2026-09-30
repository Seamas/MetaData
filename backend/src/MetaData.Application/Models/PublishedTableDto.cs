namespace MetaData.Application.Models;

/// <summary>已发布表（业务查询页可选范围）。</summary>
public class PublishedTableDto
{
    public long TableId { get; set; }

    public long ConnectionId { get; set; }

    public string ConnectionName { get; set; } = string.Empty;

    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}

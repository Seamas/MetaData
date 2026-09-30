namespace MetaData.Application.Models;

/// <summary>表元数据 DTO。</summary>
public class TableDto
{
    public long Id { get; set; }

    public long ConnectionId { get; set; }

    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public bool IsPublished { get; set; }

    public string? DefaultSortField { get; set; }

    public string? Remark { get; set; }

    public int FieldCount { get; set; }
}

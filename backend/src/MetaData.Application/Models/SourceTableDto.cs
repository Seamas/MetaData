namespace MetaData.Application.Models;

/// <summary>业务库中的表（导入前选择）。</summary>
public class SourceTableDto
{
    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string? Comment { get; set; }
}

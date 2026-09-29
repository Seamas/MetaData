namespace MetaData.Core.Models;

/// <summary>元数据导入请求中的单个表项。</summary>
public class ImportRequestItem
{
    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;
}

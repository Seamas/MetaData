namespace MetaData.Core.Models;

/// <summary>从业务库导入元数据请求。</summary>
public class MetadataImportRequest
{
    public long ConnectionId { get; set; }

    public List<ImportRequestItem> Tables { get; set; } = [];
}

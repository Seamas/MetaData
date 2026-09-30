namespace MetaData.Core.Models;

/// <summary>实时读取业务库表清单的请求（POST /api/metadata/source-tables）。</summary>
public class SourceTablesRequest
{
    public long ConnectionId { get; set; }

    /// <summary>可选 Schema 过滤；为空时由方言使用默认 Schema。</summary>
    public string? Schema { get; set; }
}

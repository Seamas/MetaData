namespace MetaData.Core.Models;

/// <summary>仅传 Id 的通用请求（删除等 POST 接口使用）。</summary>
public class IdRequest
{
    public long Id { get; set; }
}

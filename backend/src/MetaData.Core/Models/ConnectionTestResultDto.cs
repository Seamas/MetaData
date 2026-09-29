namespace MetaData.Core.Models;

/// <summary>测试连接结果。</summary>
public class ConnectionTestResultDto
{
    public bool Success { get; set; }

    public string? ServerVersion { get; set; }

    public string? Message { get; set; }
}

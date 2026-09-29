namespace MetaData.Core.Abstractions;

/// <summary>敏感信息（密码、高级连接串）加解密。</summary>
public interface ISecretProtector
{
    /// <summary>明文 → 受保护文本（带 protected: 前缀）。null/空原样返回。</summary>
    string? Protect(string? plainText);

    /// <summary>受保护文本 → 明文；非受保护内容原样返回（兼容历史数据）。</summary>
    string? Unprotect(string? protectedText);
}

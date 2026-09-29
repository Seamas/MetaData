using System.Security.Cryptography;
using MetaData.Core.Abstractions;
using MetaData.Core.Infrastructure;
using Microsoft.AspNetCore.DataProtection;

namespace MetaData.Core.Security;

/// <summary>基于 ASP.NET Core DataProtection 的敏感信息加解密。</summary>
public class DataProtectionSecretProtector : ISecretProtector
{
    private const string Prefix = "protected:";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("MetaData.Core.Secrets.v1");
    }

    public string? Protect(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        if (plainText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return plainText; // 已受保护，避免重复加密
        }

        return Prefix + _protector.Protect(plainText);
    }

    public string? Unprotect(string? protectedText)
    {
        if (string.IsNullOrEmpty(protectedText))
        {
            return protectedText;
        }

        if (!protectedText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return protectedText; // 非受保护内容（历史数据）原样返回
        }

        try
        {
            return _protector.Unprotect(protectedText[Prefix.Length..]);
        }
        catch (CryptographicException ex)
        {
            throw new BusinessException("保存的密码/连接串解密失败（可能因部署环境的密钥变更），请重新录入连接信息。")
            {
                Source = ex.Source
            };
        }
    }
}

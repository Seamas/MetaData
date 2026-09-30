using System.Data.Common;
using MetaData.Core.Entities;

namespace MetaData.Core.Services;

/// <summary>根据连接元数据创建并打开 ADO.NET 连接（内部完成驱动解析、解密、拼串）。</summary>
public interface IDbConnectionFactory
{
    Task<DbConnection> OpenAsync(DbConnectionInfo connection, CancellationToken cancellationToken = default);
}

using MetaData.Core.Entities;
using MyWebProject.Shared.Repositories;

namespace MetaData.Core.Repositories;

/// <summary>数据库连接元数据仓储。</summary>
public interface IDbConnectionInfoRepository : IRepository<DbConnectionInfo, long>
{
    /// <summary>按主键只读查询（不跟踪，用于测试连接/导入等只读执行路径）。</summary>
    Task<DbConnectionInfo?> GetByIdNoTrackingAsync(long id, CancellationToken cancellationToken = default);
}

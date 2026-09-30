using MetaData.Core.Data;
using MetaData.Core.Entities;
using MetaData.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Infrastructure.Repositories;

/// <summary>数据库连接元数据仓储实现。</summary>
internal sealed class DbConnectionInfoRepository(IMetaDataDbContext dbContext)
    : MetaDataRepositoryBase<DbConnectionInfo, long>(dbContext), IDbConnectionInfoRepository
{
    public Task<DbConnectionInfo?> GetByIdNoTrackingAsync(long id, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

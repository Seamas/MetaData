using MetaData.Core.Data;
using MetaData.Core.Entities;
using MetaData.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Infrastructure.Repositories;

/// <summary>用户字段偏好仓储实现。</summary>
internal sealed class UserFieldPreferenceRepository(IMetaDataDbContext dbContext)
    : MetaDataRepositoryBase<UserFieldPreference, long>(dbContext), IUserFieldPreferenceRepository
{
    public Task<List<UserFieldPreference>> ListByUserTableAsync(
        string userId,
        long tableId,
        CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.TableId == tableId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync(cancellationToken);

    public Task<List<UserFieldPreference>> ListByUserTableForUpdateAsync(
        string userId,
        long tableId,
        CancellationToken cancellationToken = default)
        => DbSet.Where(x => x.UserId == userId && x.TableId == tableId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync(cancellationToken);
}

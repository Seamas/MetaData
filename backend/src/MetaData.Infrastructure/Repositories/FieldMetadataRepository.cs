using MetaData.Core.Data;
using MetaData.Core.Entities;
using MetaData.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Infrastructure.Repositories;

/// <summary>字段元数据仓储实现。</summary>
internal sealed class FieldMetadataRepository(IMetaDataDbContext dbContext)
    : MetaDataRepositoryBase<FieldMetadata, long>(dbContext), IFieldMetadataRepository
{
    public Task<List<FieldMetadata>> ListByTableAsync(long tableId, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking()
            .Where(x => x.TableId == tableId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync(cancellationToken);

    public Task<List<FieldMetadata>> ListByTableForUpdateAsync(long tableId, CancellationToken cancellationToken = default)
        => DbSet.Where(x => x.TableId == tableId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<long, int>> CountByTableAsync(CancellationToken cancellationToken = default)
    {
        var counts = await DbSet.AsNoTracking()
            .GroupBy(f => f.TableId)
            .Select(g => new { TableId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.TableId, x => x.Count);
    }
}

using MetaData.Core.Data;
using MetaData.Core.Entities;
using MetaData.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Infrastructure.Repositories;

/// <summary>业务表元数据仓储实现。</summary>
internal sealed class TableMetadataRepository(IMetaDataDbContext dbContext)
    : MetaDataRepositoryBase<TableMetadata, long>(dbContext), ITableMetadataRepository
{
    public async Task<List<TableMetadata>> ListByConnectionAsync(long connectionId, CancellationToken cancellationToken = default)
    {
        var queryable = DbSet.AsNoTracking();
        if (connectionId > 0)
        {
            queryable = queryable.Where(x => x.ConnectionId == connectionId);
        }

        return await queryable
            .OrderBy(x => x.ConnectionId)
            .ThenBy(x => x.Schema)
            .ThenBy(x => x.TableName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TableMetadata>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .Include(t => t.Connection)
            .Where(t => t.IsPublished && t.Connection != null && t.Connection.IsEnabled)
            .OrderBy(t => t.Connection!.Name)
            .ThenBy(t => t.DisplayName)
            .ThenBy(t => t.TableName)
            .ToListAsync(cancellationToken);
    }

    public Task<TableMetadata?> GetWithConnectionAsync(long id, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().Include(t => t.Connection).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TableMetadata?> FindByPhysicalNameAsync(
        long connectionId,
        string? schema,
        string tableName,
        CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(
            x => x.ConnectionId == connectionId && x.Schema == schema && x.TableName == tableName,
            cancellationToken);
}

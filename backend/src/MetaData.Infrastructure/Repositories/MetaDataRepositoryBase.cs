using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using MetaData.Core.Data;
using Microsoft.EntityFrameworkCore;
using Wang.Seamas.Shared.DTOs;
using Wang.Seamas.Shared.Repositories;

namespace MetaData.Infrastructure.Repositories;

/// <summary>
/// 模块仓储基类：面向 <see cref="IMetaDataDbContext"/> 契约实现通用读写，
/// 不依赖宿主的具体 DbContext 类型。
/// </summary>
internal abstract class MetaDataRepositoryBase<T, TKey>(IMetaDataDbContext dbContext)
    : IRepository<T, TKey> where T : class
{
    protected DbSet<T> DbSet => dbContext.Set<T>();

    public Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => DbSet.FindAsync([id], cancellationToken).AsTask();

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken = default)
    {
        var queryable = DbSet.AsQueryable();
        if (predicate is not null)
        {
            queryable = queryable.Where(predicate);
        }
        return await queryable.ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(predicate, cancellationToken);

    public Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => DbSet.LongCountAsync(predicate, cancellationToken);

    public async Task<Wang.Seamas.Shared.DTOs.PagedResult<T>> GetPagedListAsync(
        Expression<Func<T, bool>>? predicate,
        PagedQuery? pagedQuery,
        CancellationToken cancellationToken = default)
    {
        var queryable = DbSet.AsQueryable();
        if (predicate is not null)
        {
            queryable = queryable.Where(predicate);
        }

        var total = await queryable.LongCountAsync(cancellationToken);
        pagedQuery ??= new PagedQuery();

        // System.Linq.Dynamic.Core 的动态排序
        if (!string.IsNullOrEmpty(pagedQuery.SortField))
        {
            queryable = queryable.OrderBy($"{pagedQuery.SortField} {(pagedQuery.IsAscending ? "asc" : "desc")}");
        }

        var items = await queryable
            .Skip(pagedQuery.SkipCount)
            .Take(pagedQuery.PageSize)
            .ToListAsync(cancellationToken);

        return new Wang.Seamas.Shared.DTOs.PagedResult<T>(items, total, pagedQuery.PageIndex, pagedQuery.PageSize);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await DbSet.AddRangeAsync(entities, cancellationToken);

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        DbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        DbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task RemoveRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await DbSet.Where(predicate).ExecuteDeleteAsync(cancellationToken);
}

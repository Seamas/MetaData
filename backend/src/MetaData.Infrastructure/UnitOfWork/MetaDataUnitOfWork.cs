using MetaData.Core.Data;
using Wang.Seamas.Shared.UnitOfWork;

namespace MetaData.Infrastructure.UnitOfWork;

/// <summary>基于 <see cref="IMetaDataDbContext"/> 的工作单元实现。</summary>
internal sealed class MetaDataUnitOfWork(IMetaDataDbContext dbContext) : IUnitOfWork
{
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;

    public bool HasActiveTransaction => _transaction is not null;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await dbContext.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_transaction is not null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}

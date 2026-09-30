using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MetaData.Core.Data;

/// <summary>
/// MetaData 模块的数据访问契约。多模块组合时由宿主的集成 DbContext 实现；
/// 模块自身不提供 DbContext，表结构由 Infrastructure 的 IEntityTypeConfiguration 类经宿主 OnModelCreating 加载。
/// </summary>
public interface IMetaDataDbContext
{
    DbSet<DbConnectionInfo> Connections { get; }

    DbSet<TableMetadata> Tables { get; }

    DbSet<FieldMetadata> Fields { get; }

    DbSet<UserFieldPreference> UserFieldPreferences { get; }

    /// <summary>泛型集合，供仓储基类实现通用读写。</summary>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>开启数据库事务（工作单元使用）。</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

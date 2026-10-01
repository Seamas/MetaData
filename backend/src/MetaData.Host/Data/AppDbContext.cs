using MetaData.Core.Data;
using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Wang.Seamas.Shared.Entities;

namespace MetaData.Host.Data;

/// <summary>
/// 宿主集成 DbContext：多模块共享。实现各业务模块的 I*DbContext 契约，
/// OnModelCreating 中依次调用各模块的 Configure* 表结构注册扩展。
/// Set&lt;TEntity&gt; 与 SaveChangesAsync 直接复用 DbContext 基类成员满足模块契约。
/// </summary>
public class AppDbContext : DbContext, IMetaDataDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DbConnectionInfo> Connections => Set<DbConnectionInfo>();

    public DbSet<TableMetadata> Tables => Set<TableMetadata>();

    public DbSet<FieldMetadata> Fields => Set<FieldMetadata>();

    public DbSet<UserFieldPreference> UserFieldPreferences => Set<UserFieldPreference>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken);

    /// <summary>审计时间戳统一填充：新增写 CreateAt/UpdateAt，更新写 UpdateAt。</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreateAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdateAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 各业务模块的实体配置：新增模块时在此追加其配置所在的程序集
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MetaData.Infrastructure.Data.Configurations.DbConnectionInfoConfiguration).Assembly);
    }
}

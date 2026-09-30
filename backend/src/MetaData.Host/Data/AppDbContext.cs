using MetaData.Core.Data;
using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Host.Data;

/// <summary>
/// 宿主集成 DbContext：多模块共享。实现各业务模块的 I*DbContext 契约，
/// OnModelCreating 中依次调用各模块的 Configure* 表结构注册扩展。
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ConfigureMetaData();
}

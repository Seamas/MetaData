using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Core.Data;

/// <summary>元数据库上下文。仅依赖 EF Core 关系型抽象，具体提供程序由宿主配置。</summary>
public class MetaDataDbContext : DbContext
{
    public MetaDataDbContext(DbContextOptions<MetaDataDbContext> options) : base(options)
    {
    }

    public DbSet<DbConnectionInfo> Connections => Set<DbConnectionInfo>();

    public DbSet<TableMetadata> Tables => Set<TableMetadata>();

    public DbSet<FieldMetadata> Fields => Set<FieldMetadata>();

    public DbSet<UserFieldPreference> UserFieldPreferences => Set<UserFieldPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var conn = modelBuilder.Entity<DbConnectionInfo>();
        conn.ToTable("md_connection");
        conn.HasKey(x => x.Id);
        conn.Property(x => x.Name).HasMaxLength(100).IsRequired();
        conn.Property(x => x.Host).HasMaxLength(200);
        conn.Property(x => x.DatabaseName).HasMaxLength(200);
        conn.Property(x => x.UserName).HasMaxLength(100);
        conn.Property(x => x.PasswordProtected).HasMaxLength(1000);
        conn.Property(x => x.AdvancedConnectionStringProtected).HasMaxLength(2000);
        conn.Property(x => x.InstanceName).HasMaxLength(100);
        conn.Property(x => x.ExtraOptions).HasMaxLength(1000);
        conn.Property(x => x.ServerVersion).HasMaxLength(200);
        conn.Property(x => x.DefaultSchema).HasMaxLength(100);
        conn.Property(x => x.Remark).HasMaxLength(500);
        conn.HasIndex(x => x.Name).IsUnique();

        var table = modelBuilder.Entity<TableMetadata>();
        table.ToTable("md_table");
        table.HasKey(x => x.Id);
        table.Property(x => x.Schema).HasMaxLength(100);
        table.Property(x => x.TableName).HasMaxLength(200).IsRequired();
        table.Property(x => x.DisplayName).HasMaxLength(200);
        table.Property(x => x.DefaultSortField).HasMaxLength(200);
        table.Property(x => x.Remark).HasMaxLength(500);
        table.HasIndex(x => new { x.ConnectionId, x.Schema, x.TableName }).IsUnique();
        table.HasOne(x => x.Connection)
             .WithMany(x => x.Tables)
             .HasForeignKey(x => x.ConnectionId)
             .OnDelete(DeleteBehavior.Cascade);

        var field = modelBuilder.Entity<FieldMetadata>();
        field.ToTable("md_field");
        field.HasKey(x => x.Id);
        field.Property(x => x.FieldName).HasMaxLength(200).IsRequired();
        field.Property(x => x.Alias).HasMaxLength(200).IsRequired();
        field.Property(x => x.DisplayName).HasMaxLength(200);
        field.Property(x => x.NativeDataType).HasMaxLength(200);
        field.Property(x => x.Remark).HasMaxLength(500);
        field.HasIndex(x => new { x.TableId, x.FieldName }).IsUnique();
        field.HasIndex(x => new { x.TableId, x.Alias }).IsUnique();
        field.HasOne(x => x.Table)
             .WithMany(x => x.Fields)
             .HasForeignKey(x => x.TableId)
             .OnDelete(DeleteBehavior.Cascade);

        var pref = modelBuilder.Entity<UserFieldPreference>();
        pref.ToTable("md_user_field_preference");
        pref.HasKey(x => x.Id);
        pref.Property(x => x.UserId).HasMaxLength(100).IsRequired();
        pref.HasIndex(x => new { x.UserId, x.TableId, x.FieldId }).IsUnique();
        pref.HasIndex(x => new { x.UserId, x.TableId });
    }
}

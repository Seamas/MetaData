using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetaData.Infrastructure.Data.Configurations;

/// <summary>数据库连接元数据表映射。</summary>
public class DbConnectionInfoConfiguration : IEntityTypeConfiguration<DbConnectionInfo>
{
    public void Configure(EntityTypeBuilder<DbConnectionInfo> builder)
    {
        builder.ToTable("md_connection");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Host).HasMaxLength(200);
        builder.Property(x => x.DatabaseName).HasMaxLength(200);
        builder.Property(x => x.UserName).HasMaxLength(100);
        builder.Property(x => x.PasswordProtected).HasMaxLength(1000);
        builder.Property(x => x.AdvancedConnectionStringProtected).HasMaxLength(2000);
        builder.Property(x => x.InstanceName).HasMaxLength(100);
        builder.Property(x => x.ExtraOptions).HasMaxLength(1000);
        builder.Property(x => x.ServerVersion).HasMaxLength(200);
        builder.Property(x => x.DefaultSchema).HasMaxLength(100);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

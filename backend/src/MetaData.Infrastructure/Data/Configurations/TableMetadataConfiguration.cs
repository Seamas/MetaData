using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetaData.Infrastructure.Data.Configurations;

/// <summary>业务表元数据表映射。</summary>
public class TableMetadataConfiguration : IEntityTypeConfiguration<TableMetadata>
{
    public void Configure(EntityTypeBuilder<TableMetadata> builder)
    {
        builder.ToTable("md_table");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Schema).HasMaxLength(100);
        builder.Property(x => x.TableName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.DefaultSortField).HasMaxLength(200);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.HasIndex(x => new { x.ConnectionId, x.Schema, x.TableName }).IsUnique();
        builder.HasOne(x => x.Connection)
               .WithMany(x => x.Tables)
               .HasForeignKey(x => x.ConnectionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

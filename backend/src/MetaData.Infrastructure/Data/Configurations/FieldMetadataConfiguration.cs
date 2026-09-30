using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetaData.Infrastructure.Data.Configurations;

/// <summary>字段元数据表映射。</summary>
public class FieldMetadataConfiguration : IEntityTypeConfiguration<FieldMetadata>
{
    public void Configure(EntityTypeBuilder<FieldMetadata> builder)
    {
        builder.ToTable("md_field");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FieldName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Alias).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.NativeDataType).HasMaxLength(200);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.HasIndex(x => new { x.TableId, x.FieldName }).IsUnique();
        builder.HasIndex(x => new { x.TableId, x.Alias }).IsUnique();
        builder.HasOne(x => x.Table)
               .WithMany(x => x.Fields)
               .HasForeignKey(x => x.TableId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

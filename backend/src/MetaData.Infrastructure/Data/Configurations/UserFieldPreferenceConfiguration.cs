using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetaData.Infrastructure.Data.Configurations;

/// <summary>用户字段偏好表映射。</summary>
public class UserFieldPreferenceConfiguration : IEntityTypeConfiguration<UserFieldPreference>
{
    public void Configure(EntityTypeBuilder<UserFieldPreference> builder)
    {
        builder.ToTable("md_user_field_preference");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.TableId, x.FieldId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.TableId });
    }
}

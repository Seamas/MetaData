using MetaData.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Core.Data;

/// <summary>
/// MetaData 模块的数据访问契约。多模块组合时由宿主的集成 DbContext 实现；
/// 模块自身不提供 DbContext，表结构通过 <see cref="MetaDataModelBuilderExtensions.ConfigureMetaData"/> 注册。
/// </summary>
public interface IMetaDataDbContext
{
    DbSet<DbConnectionInfo> Connections { get; }

    DbSet<TableMetadata> Tables { get; }

    DbSet<FieldMetadata> Fields { get; }

    DbSet<UserFieldPreference> UserFieldPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

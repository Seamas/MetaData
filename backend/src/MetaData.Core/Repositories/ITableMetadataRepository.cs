using MetaData.Core.Entities;
using Wang.Seamas.Shared.Repositories;

namespace MetaData.Core.Repositories;

/// <summary>业务表元数据仓储。</summary>
public interface ITableMetadataRepository : IRepository<TableMetadata, long>
{
    /// <summary>按连接列出表（connectionId 小于等于 0 时取全部），按连接/Schema/表名排序，不跟踪。</summary>
    Task<List<TableMetadata>> ListByConnectionAsync(long connectionId, CancellationToken cancellationToken = default);

    /// <summary>列出已发布且所属连接启用的表（含 Connection），按连接名/显示名/表名排序，不跟踪。</summary>
    Task<List<TableMetadata>> ListPublishedAsync(CancellationToken cancellationToken = default);

    /// <summary>按主键读取表并附带连接信息，不跟踪。</summary>
    Task<TableMetadata?> GetWithConnectionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>按物理定位（连接 + Schema + 表名）查找表，跟踪（导入更新路径）。</summary>
    Task<TableMetadata?> FindByPhysicalNameAsync(long connectionId, string? schema, string tableName, CancellationToken cancellationToken = default);
}

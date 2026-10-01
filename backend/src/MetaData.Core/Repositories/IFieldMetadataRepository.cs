using MetaData.Core.Entities;
using Wang.Seamas.Shared.Repositories;

namespace MetaData.Core.Repositories;

/// <summary>字段元数据仓储。</summary>
public interface IFieldMetadataRepository : IRepository<FieldMetadata, long>
{
    /// <summary>按表读取字段（Ordinal 排序），不跟踪。</summary>
    Task<List<FieldMetadata>> ListByTableAsync(long tableId, CancellationToken cancellationToken = default);

    /// <summary>按表读取字段（Ordinal 排序），跟踪（导入/批量编辑路径）。</summary>
    Task<List<FieldMetadata>> ListByTableForUpdateAsync(long tableId, CancellationToken cancellationToken = default);

    /// <summary>统计每张表的字段数量（TableId → 数量）。</summary>
    Task<Dictionary<long, int>> CountByTableAsync(CancellationToken cancellationToken = default);
}

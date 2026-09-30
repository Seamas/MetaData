using MetaData.Application.Models;

namespace MetaData.Application.Interfaces;

/// <summary>表/字段元数据查看与编辑。</summary>
public interface IMetadataService
{
    Task<List<TableDto>> GetTablesAsync(long connectionId, CancellationToken cancellationToken = default);

    Task<List<FieldDto>> GetFieldsAsync(long tableId, CancellationToken cancellationToken = default);

    Task<long> SaveTableAsync(TableDto dto, CancellationToken cancellationToken = default);

    Task SaveFieldsAsync(SaveFieldsRequest request, CancellationToken cancellationToken = default);

    Task PublishAsync(PublishTableRequest request, CancellationToken cancellationToken = default);

    Task DeleteTableAsync(long id, CancellationToken cancellationToken = default);
}

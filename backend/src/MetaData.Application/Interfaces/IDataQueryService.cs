using MetaData.Application.Models;

namespace MetaData.Application.Interfaces;

/// <summary>元数据驱动的单表业务数据查询。</summary>
public interface IDataQueryService
{
    Task<List<PublishedTableDto>> GetPublishedTablesAsync(CancellationToken cancellationToken = default);

    Task<DataQueryResponse> QueryAsync(DataQueryRequest request, CancellationToken cancellationToken = default);
}

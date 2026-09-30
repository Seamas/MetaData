using MetaData.Application.Models;

namespace MetaData.Application.Interfaces;

/// <summary>数据库连接元数据管理与连接测试。</summary>
public interface IConnectionService
{
    Task<List<ConnectionDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<long> SaveAsync(ConnectionDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<ConnectionTestResultDto> TestAsync(ConnectionDto dto, CancellationToken cancellationToken = default);

    Task<List<SourceTableDto>> GetSourceTablesAsync(long connectionId, string? schema, CancellationToken cancellationToken = default);
}

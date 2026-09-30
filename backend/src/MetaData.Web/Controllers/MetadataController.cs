using MetaData.Core.Models;
using MetaData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Web.Controllers;

[ApiController]
[Route("api/metadata")]
public class MetadataController : ControllerBase
{
    private readonly MetadataService _metadataService;
    private readonly ConnectionService _connectionService;
    private readonly MetadataImportService _importService;

    public MetadataController(
        MetadataService metadataService,
        ConnectionService connectionService,
        MetadataImportService importService)
    {
        _metadataService = metadataService;
        _connectionService = connectionService;
        _importService = importService;
    }

    /// <summary>实时读取业务库中的表清单（用于元数据生成前选择）。</summary>
    [HttpPost("source-tables")]
    public async Task<List<SourceTableDto>> SourceTables(SourceTablesRequest request, CancellationToken cancellationToken)
        => await _connectionService.GetSourceTablesAsync(request.ConnectionId, request.Schema, cancellationToken);

    [HttpPost("import")]
    public async Task<MetadataImportResultDto> Import(MetadataImportRequest request, CancellationToken cancellationToken)
        => await _importService.ImportAsync(request, cancellationToken);

    [HttpPost("tables")]
    public async Task<List<TableDto>> GetTables(ConnectionIdRequest request, CancellationToken cancellationToken)
        => await _metadataService.GetTablesAsync(request.ConnectionId, cancellationToken);

    [HttpPost("fields")]
    public async Task<List<FieldDto>> GetFields(TableIdRequest request, CancellationToken cancellationToken)
        => await _metadataService.GetFieldsAsync(request.TableId, cancellationToken);

    [HttpPost("save-table")]
    public async Task<long> SaveTable(TableDto dto, CancellationToken cancellationToken)
        => await _metadataService.SaveTableAsync(dto, cancellationToken);

    [HttpPost("save-fields")]
    public async Task<bool> SaveFields(SaveFieldsRequest request, CancellationToken cancellationToken)
    {
        await _metadataService.SaveFieldsAsync(request, cancellationToken);
        return true;
    }

    [HttpPost("publish")]
    public async Task<bool> Publish(PublishTableRequest request, CancellationToken cancellationToken)
    {
        await _metadataService.PublishAsync(request, cancellationToken);
        return true;
    }

    [HttpPost("delete-table")]
    public async Task<bool> DeleteTable(IdRequest request, CancellationToken cancellationToken)
    {
        await _metadataService.DeleteTableAsync(request.Id, cancellationToken);
        return true;
    }
}

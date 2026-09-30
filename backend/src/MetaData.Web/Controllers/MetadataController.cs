using MetaData.Application.Interfaces;
using MetaData.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Web.Controllers;

[ApiController]
[Route("api/metadata")]
public class MetadataController(
    IMetadataService metadataService,
    IConnectionService connectionService,
    IMetadataImportService importService) : ControllerBase
{
    /// <summary>实时读取业务库中的表清单（用于元数据生成前选择）。</summary>
    [HttpPost("source-tables")]
    public async Task<List<SourceTableDto>> SourceTables(SourceTablesRequest request, CancellationToken cancellationToken)
        => await connectionService.GetSourceTablesAsync(request.ConnectionId, request.Schema, cancellationToken);

    [HttpPost("import")]
    public async Task<MetadataImportResultDto> Import(MetadataImportRequest request, CancellationToken cancellationToken)
        => await importService.ImportAsync(request, cancellationToken);

    [HttpPost("tables")]
    public async Task<List<TableDto>> GetTables(ConnectionIdRequest request, CancellationToken cancellationToken)
        => await metadataService.GetTablesAsync(request.ConnectionId, cancellationToken);

    [HttpPost("fields")]
    public async Task<List<FieldDto>> GetFields(TableIdRequest request, CancellationToken cancellationToken)
        => await metadataService.GetFieldsAsync(request.TableId, cancellationToken);

    [HttpPost("save-table")]
    public async Task<long> SaveTable(TableDto dto, CancellationToken cancellationToken)
        => await metadataService.SaveTableAsync(dto, cancellationToken);

    [HttpPost("save-fields")]
    public async Task<bool> SaveFields(SaveFieldsRequest request, CancellationToken cancellationToken)
    {
        await metadataService.SaveFieldsAsync(request, cancellationToken);
        return true;
    }

    [HttpPost("publish")]
    public async Task<bool> Publish(PublishTableRequest request, CancellationToken cancellationToken)
    {
        await metadataService.PublishAsync(request, cancellationToken);
        return true;
    }

    [HttpPost("delete-table")]
    public async Task<bool> DeleteTable(IdRequest request, CancellationToken cancellationToken)
    {
        await metadataService.DeleteTableAsync(request.Id, cancellationToken);
        return true;
    }
}

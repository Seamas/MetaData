using MetaData.Core.Models;
using MetaData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Core.Api;

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
    [HttpGet("source-tables")]
    public async Task<ActionResult<List<SourceTableDto>>> GetSourceTables(
        [FromQuery] long connectionId, [FromQuery] string? schema, CancellationToken cancellationToken)
        => await _connectionService.GetSourceTablesAsync(connectionId, schema, cancellationToken);

    [HttpPost("import")]
    public async Task<ActionResult<MetadataImportResultDto>> Import([FromBody] MetadataImportRequest request, CancellationToken cancellationToken)
        => await _importService.ImportAsync(request, cancellationToken);

    [HttpGet("tables")]
    public async Task<ActionResult<List<TableDto>>> GetTables([FromQuery] long connectionId, CancellationToken cancellationToken)
        => await _metadataService.GetTablesAsync(connectionId, cancellationToken);

    [HttpGet("fields")]
    public async Task<ActionResult<List<FieldDto>>> GetFields([FromQuery] long tableId, CancellationToken cancellationToken)
        => await _metadataService.GetFieldsAsync(tableId, cancellationToken);

    [HttpPost("save-table")]
    public async Task<ActionResult<long>> SaveTable([FromBody] TableDto dto, CancellationToken cancellationToken)
        => await _metadataService.SaveTableAsync(dto, cancellationToken);

    [HttpPost("save-fields")]
    public async Task<IActionResult> SaveFields([FromBody] SaveFieldsRequest request, CancellationToken cancellationToken)
    {
        await _metadataService.SaveFieldsAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishTableRequest request, CancellationToken cancellationToken)
    {
        await _metadataService.PublishAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost("delete-table")]
    public async Task<IActionResult> DeleteTable([FromBody] IdRequest request, CancellationToken cancellationToken)
    {
        await _metadataService.DeleteTableAsync(request.Id, cancellationToken);
        return Ok();
    }
}

using MetaData.Core.Models;
using MetaData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Core.Api;

[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly DataQueryService _queryService;
    private readonly UserPreferenceService _preferenceService;

    public DataController(DataQueryService queryService, UserPreferenceService preferenceService)
    {
        _queryService = queryService;
        _preferenceService = preferenceService;

    }

    [HttpGet("published-tables")]
    public async Task<ActionResult<List<PublishedTableDto>>> GetPublishedTables(CancellationToken cancellationToken)
        => await _queryService.GetPublishedTablesAsync(cancellationToken);

    [HttpPost("query")]
    public async Task<ActionResult<DataQueryResponse>> Query([FromBody] DataQueryRequest request, CancellationToken cancellationToken)
        => await _queryService.QueryAsync(request, cancellationToken);

    [HttpGet("preferences")]
    public async Task<ActionResult<List<FieldPreferenceDto>>> GetPreferences([FromQuery] long tableId, CancellationToken cancellationToken)
        => await _preferenceService.GetAsync(tableId, cancellationToken);

    [HttpPost("preferences/save")]
    public async Task<IActionResult> SavePreferences([FromBody] SavePreferencesRequest request, CancellationToken cancellationToken)
    {
        await _preferenceService.SaveAsync(request, cancellationToken);
        return Ok();
    }
}

using MetaData.Core.Models;
using MetaData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Web.Controllers;

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
    public async Task<List<PublishedTableDto>> GetPublishedTables(CancellationToken cancellationToken)
        => await _queryService.GetPublishedTablesAsync(cancellationToken);

    [HttpPost("query")]
    public async Task<DataQueryResponse> Query(DataQueryRequest request, CancellationToken cancellationToken)
        => await _queryService.QueryAsync(request, cancellationToken);

    [HttpPost("preferences")]
    public async Task<List<FieldPreferenceDto>> GetPreferences(TableIdRequest request, CancellationToken cancellationToken)
        => await _preferenceService.GetAsync(request.TableId, cancellationToken);

    [HttpPost("preferences/save")]
    public async Task<bool> SavePreferences(SavePreferencesRequest request, CancellationToken cancellationToken)
    {
        await _preferenceService.SaveAsync(request, cancellationToken);
        return true;
    }
}

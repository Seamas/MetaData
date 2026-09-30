using MetaData.Application.Interfaces;
using MetaData.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Web.Controllers;

[ApiController]
[Route("api/data")]
public class DataController(IDataQueryService queryService, IUserPreferenceService preferenceService) : ControllerBase
{
    [HttpGet("published-tables")]
    public async Task<List<PublishedTableDto>> GetPublishedTables(CancellationToken cancellationToken)
        => await queryService.GetPublishedTablesAsync(cancellationToken);

    [HttpPost("query")]
    public async Task<DataQueryResponse> Query(DataQueryRequest request, CancellationToken cancellationToken)
        => await queryService.QueryAsync(request, cancellationToken);

    [HttpPost("preferences")]
    public async Task<List<FieldPreferenceDto>> GetPreferences(TableIdRequest request, CancellationToken cancellationToken)
        => await preferenceService.GetAsync(request.TableId, cancellationToken);

    [HttpPost("preferences/save")]
    public async Task<bool> SavePreferences(SavePreferencesRequest request, CancellationToken cancellationToken)
    {
        await preferenceService.SaveAsync(request, cancellationToken);
        return true;
    }
}

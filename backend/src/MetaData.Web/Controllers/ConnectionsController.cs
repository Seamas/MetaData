using MetaData.Core.Models;
using MetaData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Web.Controllers;

[ApiController]
[Route("api/connections")]
public class ConnectionsController : ControllerBase
{
    private readonly ConnectionService _service;

    public ConnectionsController(ConnectionService service)
    {
        _service = service;
    }

    [HttpGet("list")]
    public async Task<List<ConnectionDto>> GetList(CancellationToken cancellationToken)
        => await _service.GetListAsync(cancellationToken);

    [HttpPost("save")]
    public async Task<long> Save(ConnectionDto dto, CancellationToken cancellationToken)
        => await _service.SaveAsync(dto, cancellationToken);

    [HttpPost("delete")]
    public async Task<bool> Delete(IdRequest request, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(request.Id, cancellationToken);
        return true;
    }

    [HttpPost("test")]
    public async Task<ConnectionTestResultDto> Test(ConnectionDto dto, CancellationToken cancellationToken)
        => await _service.TestAsync(dto, cancellationToken);
}

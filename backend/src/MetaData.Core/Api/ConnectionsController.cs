using MetaData.Core.Models;
using MetaData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Core.Api;

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
    public async Task<ActionResult<List<ConnectionDto>>> GetList(CancellationToken cancellationToken)
        => await _service.GetListAsync(cancellationToken);

    [HttpPost("save")]
    public async Task<ActionResult<long>> Save([FromBody] ConnectionDto dto, CancellationToken cancellationToken)
        => await _service.SaveAsync(dto, cancellationToken);

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] IdRequest request, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(request.Id, cancellationToken);
        return Ok();
    }

    [HttpPost("test")]
    public async Task<ActionResult<ConnectionTestResultDto>> Test([FromBody] ConnectionDto dto, CancellationToken cancellationToken)
        => await _service.TestAsync(dto, cancellationToken);
}

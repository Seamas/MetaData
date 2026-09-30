using MetaData.Application.Interfaces;
using MetaData.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Web.Controllers;

[ApiController]
[Route("api/connections")]
public class ConnectionsController(IConnectionService service) : ControllerBase
{
    [HttpGet("list")]
    public async Task<List<ConnectionDto>> GetList(CancellationToken cancellationToken)
        => await service.GetListAsync(cancellationToken);

    [HttpPost("save")]
    public async Task<long> Save(ConnectionDto dto, CancellationToken cancellationToken)
        => await service.SaveAsync(dto, cancellationToken);

    [HttpPost("delete")]
    public async Task<bool> Delete(IdRequest request, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(request.Id, cancellationToken);
        return true;
    }

    [HttpPost("test")]
    public async Task<ConnectionTestResultDto> Test(ConnectionDto dto, CancellationToken cancellationToken)
        => await service.TestAsync(dto, cancellationToken);
}

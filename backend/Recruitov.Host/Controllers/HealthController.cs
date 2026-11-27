using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Health;

namespace Recruitov.Host.Controllers;

public class HealthController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<HealthStatusDto>> GetHealth(CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetHealthQuery(), ct));
}

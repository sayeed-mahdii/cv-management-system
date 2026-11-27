using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Dashboard;

namespace Recruitov.Host.Controllers;

public class DashboardController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboard(CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetDashboardQuery(), ct));
}

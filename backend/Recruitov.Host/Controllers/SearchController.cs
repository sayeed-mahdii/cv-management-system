using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Search;

namespace Recruitov.Host.Controllers;

public class SearchController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<GlobalSearchResponseDto>> Search(
        [FromQuery] string? q,
        [FromQuery] string? type,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GlobalSearchQuery(q, type), ct));
}

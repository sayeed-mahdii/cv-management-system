using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.SupportTickets;
using Recruitov.Host.Extensions;

namespace Recruitov.Host.Controllers;

[Route("api/support-ticket")]
[AllowAnonymous]
public class SupportTicketController : BaseApiController
{
    [HttpGet("context")]
    public async Task<ActionResult<SupportTicketContextDto>> GetTicketContext(
        [FromQuery] string? positionId = null,
        CancellationToken ct = default) =>
        ToActionResult(await Mediator.Send(new GetSupportTicketContextQuery(User.GetUserId(), positionId), ct));

    [HttpPost]
    public async Task<ActionResult<SupportTicketResponseDto>> CreateTicket(
        [FromBody] CreateSupportTicketDto dto,
        CancellationToken ct = default) =>
        ToActionResult(await Mediator.Send(
            new CreateSupportTicketCommand(User.GetUserId(), Request.Headers.Referer.ToString(), dto), ct));
}

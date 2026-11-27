using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Auth;
using Recruitov.Host.Extensions;

namespace Recruitov.Host.Controllers;

public class AuthController : BaseApiController
{
    [HttpGet("me")]
    [HttpGet("/api/me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser(CancellationToken ct) =>
        ToActionResult(await Mediator.Send(
            new GetCurrentUserQuery(User.GetUserId(), User.GetEmail(), User.GetDisplayName()),
            ct));

    [HttpPut("preferences")]
    [Authorize]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesDto dto, CancellationToken ct) =>
        ToIActionResult(await Mediator.Send(
            new UpdatePreferencesCommand(User.GetUserId(), dto.PreferredLanguage, dto.PreferredTheme),
            ct));
}

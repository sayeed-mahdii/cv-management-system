using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Admin;
using Recruitov.Domain.Entities;
using Recruitov.Host.Extensions;

namespace Recruitov.Host.Controllers;

[Authorize(Roles = AppUserRole.Administrator)]
public class AdminController : BaseApiController
{
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserListItemDto>>> GetUsers(CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetAdminUsersQuery(), ct));

    [HttpPut("users/block")]
    public async Task<IActionResult> BatchBlockUsers([FromBody] BatchBlockUsersRequest request, CancellationToken ct) =>
        ToIActionResult(await Mediator.Send(new BatchBlockUsersCommand(request.UserIds, request.IsBlocked), ct));

    [HttpPut("users/{id}/roles")]
    public async Task<ActionResult<ManageRolesResponse>> UpdateUserRoles(
        string id,
        [FromBody] ManageUserRolesRequest request,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new UpdateUserRolesCommand(id, User.GetUserId(), request.Roles), ct));

    [HttpPost("users/batch-delete")]
    public async Task<IActionResult> BatchDeleteUsers([FromBody] BatchDeleteUsersRequest request, CancellationToken ct) =>
        ToIActionResult(await Mediator.Send(new BatchDeleteUsersCommand(request.UserIds), ct));

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken ct) =>
        ToIActionResult(await Mediator.Send(new DeleteUserCommand(id), ct));
}

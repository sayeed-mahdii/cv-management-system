using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Profile;
using Recruitov.Host.Extensions;

namespace Recruitov.Host.Controllers;

[Authorize]
public class ProfileController : BaseApiController
{
    [HttpGet("{userId}")]
    public async Task<ActionResult<ProfileResponseDto>> GetProfile(string userId, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(
            new GetProfileQuery(userId, User.GetUserId(), User.IsRecruiterOrAdmin()), ct));

    [HttpGet("me")]
    public Task<ActionResult<ProfileResponseDto>> GetMyProfile(CancellationToken ct) =>
        GetProfile(User.GetUserId(), ct);

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateProfile(string userId, [FromBody] UpdateProfileDto dto, CancellationToken ct) =>
        ToIActionResult(await Mediator.Send(
            new UpdateProfileCommand(userId, User.GetUserId(), User.IsAdmin(), dto), ct));

    [HttpDelete("{userId}/attributes/{attributeDefinitionId:int}")]
    public async Task<IActionResult> RemoveAttributeValue(
        string userId,
        int attributeDefinitionId,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(
            new RemoveAttributeValueCommand(userId, User.GetUserId(), User.IsAdmin(), attributeDefinitionId), ct));

    [HttpPost("{userId}/projects")]
    public async Task<ActionResult<ProjectDto>> AddProject(
        string userId,
        [FromBody] CreateProjectDto dto,
        CancellationToken ct) =>
        ToCreatedActionResult(
            await Mediator.Send(new AddProjectCommand(userId, User.GetUserId(), User.IsAdmin(), dto), ct),
            nameof(GetProfile),
            _ => new { userId });

    [HttpPut("{userId}/projects/{projectId:int}")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(
        string userId,
        int projectId,
        [FromBody] UpdateProjectDto dto,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(
            new UpdateProjectCommand(userId, User.GetUserId(), User.IsAdmin(), projectId, dto), ct));

    [HttpDelete("{userId}/projects/{projectId:int}")]
    public async Task<IActionResult> DeleteProject(string userId, int projectId, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(
            new DeleteProjectCommand(userId, User.GetUserId(), User.IsAdmin(), projectId), ct));

    [HttpGet("upload-signature")]
    public async Task<ActionResult<CloudinarySignatureDto>> GetUploadSignature(CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetUploadSignatureQuery(), ct));

    [HttpDelete("applications/{cvId:int}")]
    public async Task<IActionResult> WithdrawApplication(int cvId, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(
            new WithdrawApplicationByCvIdCommand(cvId, User.GetUserId(), User.IsAdmin()), ct));
}

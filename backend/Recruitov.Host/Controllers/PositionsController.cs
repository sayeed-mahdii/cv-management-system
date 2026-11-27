using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Positions;
using Recruitov.Host.Extensions;

namespace Recruitov.Host.Controllers;

public class PositionsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PositionSummaryDto>>> GetPositions(
        [FromQuery] PositionSearchQuery query,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetPositionsQuery(query), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PositionDetailDto>> GetPosition(int id, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetPositionByIdQuery(id, User.GetUserId()), ct));

    [HttpPost]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<PositionDetailDto>> CreatePosition(
        [FromBody] CreatePositionDto dto,
        CancellationToken ct) =>
        ToCreatedActionResult(
            await Mediator.Send(new CreatePositionCommand(dto), ct),
            nameof(GetPosition),
            created => new { id = created.Id });

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<PositionDetailDto>> UpdatePosition(
        int id,
        [FromBody] UpdatePositionDto dto,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new UpdatePositionCommand(id, dto), ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<IActionResult> DeletePosition(int id, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new DeletePositionCommand(id), ct));

    [HttpPost("{id:int}/duplicate")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<PositionDetailDto>> DuplicatePosition(int id, CancellationToken ct) =>
        ToCreatedActionResult(
            await Mediator.Send(new DuplicatePositionCommand(id), ct),
            nameof(GetPosition),
            clone => new { id = clone.Id });

    [HttpPost("{id:int}/apply")]
    [Authorize(Roles = "Candidate,Administrator")]
    public async Task<IActionResult> ApplyToPosition(int id, CancellationToken ct) =>
        ToIActionResult(await Mediator.Send(new ApplyToPositionCommand(id, User.GetUserId(), User.IsAdmin()), ct));

    [HttpDelete("{id:int}/apply")]
    [Authorize(Roles = "Candidate,Administrator")]
    public async Task<IActionResult> WithdrawApplicationForPosition(int id, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new WithdrawPositionApplicationCommand(id, User.GetUserId()), ct));

    [HttpGet("{id:int}/applicants")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<List<ApplicantSummaryDto>>> GetApplicants(int id, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetPositionApplicantsQuery(id), ct));

    [HttpGet("{id:int}/applicants/{candidateId}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<ApplicantDetailDto>> GetApplicantDetail(
        int id,
        string candidateId,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetApplicantDetailQuery(id, candidateId), ct));
}

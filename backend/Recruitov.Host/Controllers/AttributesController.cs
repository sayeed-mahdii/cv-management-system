using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Attributes;

namespace Recruitov.Host.Controllers;

[Authorize]
public class AttributesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AttributeDto>>> GetAttributes(
        [FromQuery] AttributeSearchQuery query,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetAttributesQuery(query), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AttributeDto>> GetAttribute(int id, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetAttributeByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<AttributeDto>> CreateAttribute(
        [FromBody] CreateAttributeDto dto,
        CancellationToken ct) =>
        ToCreatedActionResult(
            await Mediator.Send(new CreateAttributeCommand(dto, User.Identity?.Name), ct),
            nameof(GetAttribute),
            created => new { id = created.Id });

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<AttributeDto>> UpdateAttribute(
        int id,
        [FromBody] UpdateAttributeDto dto,
        CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new UpdateAttributeCommand(id, dto), ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<IActionResult> DeleteAttribute(int id, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new DeleteAttributeCommand(id), ct));
}

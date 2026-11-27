using Microsoft.AspNetCore.Mvc;
using Recruitov.Application.Dtos;
using Recruitov.Application.Features.Categories;

namespace Recruitov.Host.Controllers;

public class CategoriesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetCategoriesQuery(), ct));
}

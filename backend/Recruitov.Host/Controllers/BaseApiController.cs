using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Recruitov.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IMediator Mediator =>
        HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected ActionResult<T> ToActionResult<T>(ErrorOr<T> result) =>
        result.Match(value => Ok(value), Problem);

    protected ActionResult<T> ToCreatedActionResult<T>(
        ErrorOr<T> result,
        string actionName,
        Func<T, object> routeValuesFactory) =>
        result.Match(
            value => CreatedAtAction(actionName, routeValuesFactory(value), value),
            Problem);

    protected IActionResult ToIActionResult<T>(ErrorOr<T> result) =>
        result.Match<IActionResult>(value => Ok(value), Problem);

    protected IActionResult ToActionResult(ErrorOr<Deleted> result) =>
        result.Match<IActionResult>(_ => NoContent(), Problem);

    protected IActionResult ToActionResult(ErrorOr<Success> result) =>
        result.Match<IActionResult>(_ => Ok(), Problem);

    private ActionResult Problem(List<Error> errors)
    {
        var firstError = errors.First();
        var statusCode = firstError.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        // Include RFC 7807 ProblemDetails fields + `message` & custom metadata for 100% frontend compatibility
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = firstError.Code,
            Detail = firstError.Description
        };

        problem.Extensions["message"] = firstError.Description;

        if (firstError.Metadata is not null)
        {
            foreach (var (key, value) in firstError.Metadata)
            {
                problem.Extensions[key] = value;
            }
        }

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}

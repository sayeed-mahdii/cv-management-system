using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Recruitov.Host.Filters;

/// <summary>
/// Rejects any authenticated request if the user is marked IsBlocked in PostgreSQL.
/// Preserves exact `{ message, error: "AccountBlocked" }` payload expected by the React frontend.
/// </summary>
public class BlockedUserFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true &&
            user.HasClaim(c => c.Type == "IsBlocked" && c.Value == "true"))
        {
            context.Result = new ObjectResult(new
            {
                message = "Your account has been blocked by an administrator.",
                error = "AccountBlocked"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}

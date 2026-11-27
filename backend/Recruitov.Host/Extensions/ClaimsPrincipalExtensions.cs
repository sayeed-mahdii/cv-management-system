using System.Security.Claims;
using Recruitov.Domain.Entities;

namespace Recruitov.Host.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal? principal) =>
        principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? principal?.FindFirst("sub")?.Value
        ?? string.Empty;

    public static string? GetEmail(this ClaimsPrincipal? principal) =>
        principal?.FindFirst(ClaimTypes.Email)?.Value
        ?? principal?.FindFirst("email")?.Value;

    public static string? GetDisplayName(this ClaimsPrincipal? principal) =>
        principal?.FindFirst("name")?.Value
        ?? principal?.FindFirst(ClaimTypes.Name)?.Value;

    public static bool IsAdmin(this ClaimsPrincipal? principal) =>
        principal?.IsInRole(AppUserRole.Administrator) == true;

    public static bool IsRecruiterOrAdmin(this ClaimsPrincipal? principal) =>
        principal?.IsInRole(AppUserRole.Recruiter) == true ||
        principal?.IsInRole(AppUserRole.Administrator) == true;
}

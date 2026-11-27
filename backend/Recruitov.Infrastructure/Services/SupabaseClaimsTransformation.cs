using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Recruitov.Domain.Entities;
using Recruitov.Infrastructure.Data;

namespace Recruitov.Infrastructure.Services;

public class SupabaseClaimsTransformation(
    AppDbContext db,
    ILogger<SupabaseClaimsTransformation> logger) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        if (identity.HasClaim(c => c.Type == "claims_transformed"))
            return principal;

        var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? identity.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return principal;

        var email = identity.FindFirst(ClaimTypes.Email)?.Value
                    ?? identity.FindFirst("email")?.Value
                    ?? string.Empty;

        var user = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            user = new AppUser
            {
                Id = userId,
                Email = email,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
            };

            user.Roles.Add(new AppUserRole
            {
                UserId = userId,
                Role = AppUserRole.Candidate
            });

            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Auto-provisioned new candidate user: {UserId} ({Email})", userId, email);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Concurrent user auto-provisioning occurred for user {UserId}", userId);
            }
        }

        foreach (var userRole in user.Roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, userRole.Role));

        if (user.IsBlocked)
            identity.AddClaim(new Claim("IsBlocked", "true"));

        identity.AddClaim(new Claim("claims_transformed", "true"));
        return principal;
    }
}

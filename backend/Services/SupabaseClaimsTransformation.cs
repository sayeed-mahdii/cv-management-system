using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models.Entities;

namespace backend.Services;

/// <summary>
/// Transforms the Supabase JWT claims by injecting application roles from PostgreSQL.
/// Automatically provisions new users with the Candidate role on first login.
/// </summary>
public class SupabaseClaimsTransformation : IClaimsTransformation
{
    private readonly AppDbContext _db;
    private readonly ILogger<SupabaseClaimsTransformation> _logger;

    public SupabaseClaimsTransformation(AppDbContext db, ILogger<SupabaseClaimsTransformation> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        // Avoid transforming multiple times in the same request
        if (identity.HasClaim(c => c.Type == "claims_transformed"))
        {
            return principal;
        }

        var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? identity.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return principal;
        }

        var email = identity.FindFirst(ClaimTypes.Email)?.Value
                    ?? identity.FindFirst("email")?.Value
                    ?? string.Empty;

        // Fetch user and their assigned roles from PostgreSQL
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            // Auto-provision user on first authenticated request (default role: Candidate)
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

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation("Auto-provisioned new candidate user: {UserId} ({Email})", userId, email);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Concurrent user auto-provisioning occurred for user {UserId}", userId);
            }
        }

        // Add role claims so [Authorize(Roles = "Recruiter,Administrator")] works out of the box
        foreach (var userRole in user.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, userRole.Role));
        }

        // Add blocked status claim
        if (user.IsBlocked)
        {
            identity.AddClaim(new Claim("IsBlocked", "true"));
        }

        identity.AddClaim(new Claim("claims_transformed", "true"));
        return principal;
    }
}

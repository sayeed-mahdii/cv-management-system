using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models.Entities;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the currently authenticated user's profile, roles, and preferences.
    /// </summary>
    [HttpGet("me")]
    [HttpGet("/api/me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.AttributeValues)
                .ThenInclude(v => v.AttributeDefinition)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                        ?? User.FindFirst("email")?.Value
                        ?? $"{userId}@oauth.recruitov.com";

            user = new AppUser
            {
                Id = userId,
                Email = email,
                IsBlocked = false,
                PreferredLanguage = "en",
                PreferredTheme = "light",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            _db.UserRoles.Add(new AppUserRole
            {
                UserId = userId,
                Role = AppUserRole.Candidate
            });

            // Extract Name from claims if provided by OAuth provider
            var nameClaim = User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrWhiteSpace(nameClaim))
            {
                var parts = nameClaim.Trim().Split(' ', 2);
                var firstName = parts[0];
                var lastName = parts.Length > 1 ? parts[1] : string.Empty;

                _db.UserAttributeValues.Add(new UserAttributeValue
                {
                    UserId = userId,
                    AttributeDefinitionId = 1, // First Name
                    Value = firstName,
                    UpdatedAt = DateTime.UtcNow
                });

                if (!string.IsNullOrWhiteSpace(lastName))
                {
                    _db.UserAttributeValues.Add(new UserAttributeValue
                    {
                        UserId = userId,
                        AttributeDefinitionId = 2, // Last Name
                        Value = lastName,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();

            user = await _db.Users
                .Include(u => u.Roles)
                .Include(u => u.AttributeValues)
                    .ThenInclude(v => v.AttributeDefinition)
                .FirstAsync(u => u.Id == userId);
        }

        // Return user data along with their built-in "Me" attributes
        var builtInAttributes = user.AttributeValues
            .Where(v => v.AttributeDefinition != null && v.AttributeDefinition.IsBuiltIn)
            .Select(v => new
            {
                AttributeId = v.AttributeDefinitionId,
                Name = v.AttributeDefinition!.Name,
                DataType = v.AttributeDefinition.DataType.ToString(),
                Value = v.Value,
                ImagePublicId = v.ImagePublicId
            })
            .ToList();

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            isBlocked = user.IsBlocked,
            preferredLanguage = user.PreferredLanguage,
            preferredTheme = user.PreferredTheme,
            version = user.Version,
            roles = user.Roles.Select(r => r.Role).ToList(),
            meAttributes = builtInAttributes
        });
    }

    /// <summary>
    /// Updates the user's UI preferences (language and theme).
    /// Uses ExecuteUpdateAsync so bookkeeping doesn't trigger optimistic lock version bumps.
    /// </summary>
    [HttpPut("preferences")]
    [Authorize]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PreferredLanguage, dto.PreferredLanguage ?? "en")
                .SetProperty(u => u.PreferredTheme, dto.PreferredTheme ?? "light"));

        return Ok(new { message = "Preferences updated." });
    }
}

public class UpdatePreferencesDto
{
    public string? PreferredLanguage { get; set; }
    public string? PreferredTheme { get; set; }
}

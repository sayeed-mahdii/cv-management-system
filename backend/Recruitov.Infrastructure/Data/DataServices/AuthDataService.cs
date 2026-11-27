using Microsoft.EntityFrameworkCore;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;

namespace Recruitov.Infrastructure.Data.DataServices;

public class AuthDataService(AppDbContext db) : IAuthDataService
{
    public async Task<CurrentUserDto> GetOrProvisionCurrentUserAsync(
        string userId,
        string? email,
        string? displayName,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.Roles)
            .Include(u => u.AttributeValues)
                .ThenInclude(v => v.AttributeDefinition)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            user = new AppUser
            {
                Id = userId,
                Email = !string.IsNullOrEmpty(email) ? email : $"{userId}@oauth.recruitov.com",
                IsBlocked = false,
                PreferredLanguage = "en",
                PreferredTheme = "light",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            db.UserRoles.Add(new AppUserRole
            {
                UserId = userId,
                Role = AppUserRole.Candidate
            });

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                var parts = displayName.Trim().Split(' ', 2);
                db.UserAttributeValues.Add(new UserAttributeValue
                {
                    UserId = userId,
                    AttributeDefinitionId = 1,
                    Value = parts[0],
                    UpdatedAt = DateTime.UtcNow
                });

                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    db.UserAttributeValues.Add(new UserAttributeValue
                    {
                        UserId = userId,
                        AttributeDefinitionId = 2,
                        Value = parts[1],
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await db.SaveChangesAsync(ct);

            user = await db.Users
                .Include(u => u.Roles)
                .Include(u => u.AttributeValues)
                    .ThenInclude(v => v.AttributeDefinition)
                .FirstAsync(u => u.Id == userId, ct);
        }

        var builtInAttributes = user.AttributeValues
            .Where(v => v.AttributeDefinition != null && v.AttributeDefinition.IsBuiltIn)
            .Select(v => new BuiltInAttributeItemDto
            {
                AttributeId = v.AttributeDefinitionId,
                Name = v.AttributeDefinition!.Name,
                DataType = v.AttributeDefinition.DataType.ToString(),
                Value = v.Value,
                ImagePublicId = v.ImagePublicId
            })
            .ToList();

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            IsBlocked = user.IsBlocked,
            PreferredLanguage = user.PreferredLanguage,
            PreferredTheme = user.PreferredTheme,
            Version = user.Version,
            Roles = user.Roles.Select(r => r.Role).ToList(),
            MeAttributes = builtInAttributes
        };
    }

    public Task UpdatePreferencesAsync(string userId, string language, string theme, CancellationToken ct = default) =>
        db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PreferredLanguage, language)
                .SetProperty(u => u.PreferredTheme, theme), ct);
}

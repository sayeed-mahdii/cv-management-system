using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;

namespace Recruitov.Infrastructure.Data.DataServices;

public class AdminDataService(
    AppDbContext db,
    ILogger<AdminDataService> logger) : IAdminDataService
{
    public async Task<List<AdminUserListItemDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var rawUsers = await db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.IsBlocked,
                u.CreatedAt,
                u.Version,
                Roles = u.Roles.Select(r => r.Role).ToList(),
                FirstName = u.AttributeValues
                    .Where(v => v.AttributeDefinitionId == 1 || (v.AttributeDefinition != null && v.AttributeDefinition.Name.ToLower() == "first name"))
                    .Select(v => v.Value)
                    .FirstOrDefault(),
                LastName = u.AttributeValues
                    .Where(v => v.AttributeDefinitionId == 2 || (v.AttributeDefinition != null && v.AttributeDefinition.Name.ToLower() == "last name"))
                    .Select(v => v.Value)
                    .FirstOrDefault(),
                ProjectsCount = u.Projects.Count,
                ApplicationsCount = u.Cvs.Count
            })
            .ToListAsync(ct);

        return rawUsers.Select(u =>
        {
            var fullName = $"{u.FirstName ?? ""} {u.LastName ?? ""}".Trim();
            return new AdminUserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                IsBlocked = u.IsBlocked,
                Roles = u.Roles.Count > 0 ? u.Roles : [AppUserRole.Candidate],
                CandidateName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                ProjectsCount = u.ProjectsCount,
                ApplicationsCount = u.ApplicationsCount,
                CreatedAt = u.CreatedAt,
                Version = u.Version
            };
        }).ToList();
    }

    public async Task<ErrorOr<(int AffectedCount, bool IsBlocked)>> BatchBlockUsersAsync(
        List<string> userIds,
        bool isBlocked,
        CancellationToken ct = default)
    {
        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);

        if (users.Count == 0)
            return Error.NotFound("Admin.UsersNotFound", "No matching users found.");

        foreach (var user in users)
        {
            user.IsBlocked = isBlocked;
            user.Version++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated IsBlocked to {IsBlocked} for {Count} user(s)", isBlocked, users.Count);

        return (users.Count, isBlocked);
    }

    public async Task<ErrorOr<ManageRolesResponse>> UpdateUserRolesAsync(
        string targetUserId,
        string callerId,
        List<string> roles,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct);

        if (user is null)
            return Error.NotFound("Admin.UserNotFound", "User not found.");

        var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AppUserRole.Candidate,
            AppUserRole.Recruiter,
            AppUserRole.Administrator
        };

        var requestedRoles = roles
            .Where(r => allowedRoles.Contains(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedRoles.Count == 0)
            requestedRoles.Add(AppUserRole.Candidate);

        var toRemove = user.Roles
            .Where(existing => !requestedRoles.Contains(existing.Role, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var r in toRemove)
            db.UserRoles.Remove(r);

        var existingRoleNames = user.Roles.Select(r => r.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var reqRole in requestedRoles)
        {
            if (!existingRoleNames.Contains(reqRole))
            {
                db.UserRoles.Add(new AppUserRole { UserId = user.Id, Role = reqRole });
            }
        }

        user.Version++;
        await db.SaveChangesAsync(ct);

        var callerLostAdmin = callerId == targetUserId &&
                              !requestedRoles.Contains(AppUserRole.Administrator, StringComparer.OrdinalIgnoreCase);

        logger.LogInformation(
            "Admin {AdminId} updated roles for user {TargetUserId} to [{Roles}]. CallerLostAdmin: {CallerLostAdmin}",
            callerId, targetUserId, string.Join(", ", requestedRoles), callerLostAdmin);

        return new ManageRolesResponse
        {
            UserId = user.Id,
            Roles = requestedRoles,
            CallerLostAdmin = callerLostAdmin
        };
    }

    public async Task<ErrorOr<int>> BatchDeleteUsersAsync(List<string> userIds, CancellationToken ct = default)
    {
        var users = await db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(ct);
        if (users.Count == 0)
            return Error.NotFound("Admin.UsersNotFound", "No matching users found.");

        db.Users.RemoveRange(users);
        await db.SaveChangesAsync(ct);
        logger.LogWarning("Batch deleted {Count} user(s)", users.Count);

        return users.Count;
    }

    public async Task<ErrorOr<Success>> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Error.NotFound("Admin.UserNotFound", "User not found.");

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        logger.LogWarning("Deleted user {TargetUserId}", userId);

        return Result.Success;
    }
}

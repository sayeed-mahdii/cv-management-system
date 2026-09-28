using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models.DTOs;
using backend.Models.Entities;

namespace backend.Controllers;

/// <summary>
/// Handles administrative user management, account blocking/unblocking, role management,
/// and cascade user deletion.
/// Strictly restricted to users with the Administrator role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppUserRole.Administrator)]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string GetCurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? string.Empty;

    /// <summary>
    /// Returns the complete roster of registered platform users for the Admin table.
    /// Single round-trip query without N+1 iterations.
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserListItemDto>>> GetUsers()
    {
        var rawUsers = await _db.Users
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
            .ToListAsync();

        var result = rawUsers.Select(u =>
        {
            var fullName = $"{u.FirstName ?? ""} {u.LastName ?? ""}".Trim();
            return new AdminUserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                IsBlocked = u.IsBlocked,
                Roles = u.Roles.Count > 0 ? u.Roles : new List<string> { AppUserRole.Candidate },
                CandidateName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                ProjectsCount = u.ProjectsCount,
                ApplicationsCount = u.ApplicationsCount,
                CreatedAt = u.CreatedAt,
                Version = u.Version
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Bulk or single user blocking/unblocking.
    /// Immediately disables candidate/recruiter write operations across the system.
    /// </summary>
    [HttpPut("users/block")]
    public async Task<ActionResult> BatchBlockUsers([FromBody] BatchBlockUsersRequest request)
    {
        if (request.UserIds == null || request.UserIds.Count == 0)
        {
            return BadRequest(new { message = "At least one user ID must be provided." });
        }

        var users = await _db.Users
            .Where(u => request.UserIds.Contains(u.Id))
            .ToListAsync();

        if (users.Count == 0)
        {
            return NotFound(new { message = "No matching users found." });
        }

        foreach (var user in users)
        {
            user.IsBlocked = request.IsBlocked;
            user.Version++;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Admin {AdminId} updated IsBlocked to {IsBlocked} for {Count} user(s)",
            GetCurrentUserId(), request.IsBlocked, users.Count);

        return Ok(new
        {
            message = $"Successfully updated {users.Count} user(s).",
            affectedCount = users.Count,
            isBlocked = request.IsBlocked
        });
    }

    /// <summary>
    /// Updates roles for a specified user.
    /// Admins may assign or remove Candidate, Recruiter, or Administrator roles.
    /// Explicitly supports admins removing their own Administrator role.
    /// </summary>
    [HttpPut("users/{id}/roles")]
    public async Task<ActionResult<ManageRolesResponse>> UpdateUserRoles(
        string id,
        [FromBody] ManageUserRolesRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Validate valid roles
        var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AppUserRole.Candidate,
            AppUserRole.Recruiter,
            AppUserRole.Administrator
        };

        var requestedRoles = (request.Roles ?? new List<string>())
            .Where(r => allowedRoles.Contains(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // If no roles specified, default to Candidate
        if (requestedRoles.Count == 0)
        {
            requestedRoles.Add(AppUserRole.Candidate);
        }

        // Remove unselected roles
        var toRemove = user.Roles
            .Where(existing => !requestedRoles.Contains(existing.Role, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var r in toRemove)
        {
            _db.UserRoles.Remove(r);
        }

        // Add newly selected roles
        var existingRoleNames = user.Roles.Select(r => r.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var reqRole in requestedRoles)
        {
            if (!existingRoleNames.Contains(reqRole))
            {
                _db.UserRoles.Add(new AppUserRole
                {
                    UserId = user.Id,
                    Role = reqRole
                });
            }
        }

        user.Version++;
        await _db.SaveChangesAsync();

        var callerId = GetCurrentUserId();
        var callerLostAdmin = callerId == id && !requestedRoles.Contains(AppUserRole.Administrator, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Admin {AdminId} updated roles for user {TargetUserId} to [{Roles}]. CallerLostAdmin: {CallerLostAdmin}",
            callerId, id, string.Join(", ", requestedRoles), callerLostAdmin);

        return Ok(new ManageRolesResponse
        {
            UserId = user.Id,
            Roles = requestedRoles,
            CallerLostAdmin = callerLostAdmin
        });
    }

    /// <summary>
    /// Bulk deletes users and cascades deletion through EF Core.
    /// </summary>
    [HttpPost("users/batch-delete")]
    public async Task<ActionResult> BatchDeleteUsers([FromBody] BatchDeleteUsersRequest request)
    {
        if (request.UserIds == null || request.UserIds.Count == 0)
        {
            return BadRequest(new { message = "At least one user ID must be provided." });
        }

        var users = await _db.Users
            .Where(u => request.UserIds.Contains(u.Id))
            .ToListAsync();

        if (users.Count == 0)
        {
            return NotFound(new { message = "No matching users found." });
        }

        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();

        _logger.LogWarning(
            "Admin {AdminId} deleted {Count} user(s)",
            GetCurrentUserId(), users.Count);

        return Ok(new
        {
            message = $"Successfully deleted {users.Count} user(s).",
            deletedCount = users.Count
        });
    }

    /// <summary>
    /// Single user deletion.
    /// </summary>
    [HttpDelete("users/{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        _logger.LogWarning("Admin {AdminId} deleted user {TargetUserId}", GetCurrentUserId(), id);

        return Ok(new { message = "User deleted successfully." });
    }
}

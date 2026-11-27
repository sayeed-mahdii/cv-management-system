using Microsoft.EntityFrameworkCore;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;

namespace Recruitov.Infrastructure.Data.DataServices;

public class SupportTicketDataService(AppDbContext db) : ISupportTicketDataService
{
    public async Task<(string ReporterName, string UserRole)> ResolveUserInfoAsync(string? userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ("Guest", "Visitor");

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.AttributeValues).ThenInclude(v => v.AttributeDefinition)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return ("Unknown User", "User");

        var firstName = user.AttributeValues
            .Where(v => v.AttributeDefinitionId == 1 || (v.AttributeDefinition != null && v.AttributeDefinition.Name.ToLower() == "first name"))
            .Select(v => v.Value)
            .FirstOrDefault();

        var lastName = user.AttributeValues
            .Where(v => v.AttributeDefinitionId == 2 || (v.AttributeDefinition != null && v.AttributeDefinition.Name.ToLower() == "last name"))
            .Select(v => v.Value)
            .FirstOrDefault();

        var fullName = $"{firstName ?? ""} {lastName ?? ""}".Trim();
        if (string.IsNullOrWhiteSpace(fullName)) fullName = user.Email;

        var roles = user.Roles.Select(r => r.Role).ToList();
        var primaryRole = roles.Contains(AppUserRole.Administrator)
            ? AppUserRole.Administrator
            : roles.Contains(AppUserRole.Recruiter)
                ? AppUserRole.Recruiter
                : AppUserRole.Candidate;

        return (fullName, primaryRole);
    }

    public async Task<List<string>> GetActiveAdminEmailsAsync(CancellationToken ct = default)
    {
        var adminEmails = await db.Users
            .AsNoTracking()
            .Where(u => !u.IsBlocked && u.Roles.Any(r => r.Role == AppUserRole.Administrator))
            .Select(u => u.Email)
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct()
            .ToListAsync(ct);

        if (adminEmails.Count == 0)
            adminEmails.Add("admin@recruitov.com");

        return adminEmails;
    }

    public Task<string?> GetPositionTitleAsync(int positionId, CancellationToken ct = default) =>
        db.Positions
            .AsNoTracking()
            .Where(p => p.Id == positionId)
            .Select(p => p.Title)
            .FirstOrDefaultAsync(ct);
}

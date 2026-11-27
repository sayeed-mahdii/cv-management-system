using Microsoft.EntityFrameworkCore;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;

namespace Recruitov.Infrastructure.Data.DataServices;

public class DashboardDataService(AppDbContext db) : IDashboardDataService
{
    public async Task<DashboardResponseDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

        var totalPositions = await db.Positions.CountAsync(ct);
        var totalApplications = await db.Cvs.CountAsync(ct);
        var appsLast24h = await db.Cvs.CountAsync(c => c.CreatedAt >= twentyFourHoursAgo, ct);
        var totalCandidates = await db.UserRoles.CountAsync(r => r.Role == AppUserRole.Candidate, ct);
        var totalRecruiters = await db.UserRoles.CountAsync(r => r.Role == AppUserRole.Recruiter, ct);

        var latestPositions = await db.Positions
            .AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .Take(5)
            .Select(p => new PositionSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                ShortDescription = p.ShortDescription,
                IsPublic = p.IsPublic,
                MaxProjects = p.MaxProjects,
                Version = p.Version,
                ApplicationsCount = p.Cvs.Count,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Tags = p.PositionTags.Select(pt => pt.Tag!.Name).ToList(),
                AttributeNames = p.PositionAttributes.Select(pa => pa.AttributeDefinition!.Name).ToList()
            })
            .ToListAsync(ct);

        var popularPositions = await db.Positions
            .AsNoTracking()
            .OrderByDescending(p => p.Cvs.Count)
            .ThenByDescending(p => p.UpdatedAt)
            .Take(5)
            .Select(p => new PositionSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                ShortDescription = p.ShortDescription,
                IsPublic = p.IsPublic,
                MaxProjects = p.MaxProjects,
                Version = p.Version,
                ApplicationsCount = p.Cvs.Count,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Tags = p.PositionTags.Select(pt => pt.Tag!.Name).ToList(),
                AttributeNames = p.PositionAttributes.Select(pa => pa.AttributeDefinition!.Name).ToList()
            })
            .ToListAsync(ct);

        var tagCloud = await db.Tags
            .AsNoTracking()
            .Select(t => new TagCloudItemDto
            {
                Value = t.Name,
                Count = t.PositionTags.Count + t.ProjectTags.Count
            })
            .Where(t => t.Count > 0)
            .OrderByDescending(t => t.Count)
            .Take(30)
            .ToListAsync(ct);

        return new DashboardResponseDto
        {
            Stats = new PlatformStatsDto
            {
                TotalPositions = totalPositions,
                TotalApplications = totalApplications,
                ApplicationsLast24Hours = appsLast24h,
                TotalCandidates = totalCandidates,
                TotalRecruiters = totalRecruiters
            },
            LatestPositions = latestPositions,
            PopularPositions = popularPositions,
            TagCloud = tagCloud
        };
    }
}

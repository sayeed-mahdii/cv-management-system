using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models.DTOs;
using backend.Models.Entities;
using backend.Models.Enums;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(AppDbContext db, ILogger<DashboardController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns aggregated main page statistics, latest positions, top popular positions,
    /// and technology tag cloud (Spec §4.1).
    /// Single batch query without loop scans. Open to unauthenticated public visitors.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboard()
    {
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

        // 1. Platform Statistics (Count queries)
        var totalPositions = await _db.Positions.CountAsync();
        var totalApplications = await _db.Cvs.CountAsync();
        var appsLast24h = await _db.Cvs.CountAsync(c => c.CreatedAt >= twentyFourHoursAgo);
        var totalCandidates = await _db.UserRoles.CountAsync(r => r.Role == AppUserRole.Candidate);
        var totalRecruiters = await _db.UserRoles.CountAsync(r => r.Role == AppUserRole.Recruiter);

        // 2. Latest 5 Positions (sorted by UpdatedAt desc)
        var latestPositions = await _db.Positions
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
            .ToListAsync();

        // 3. Top 5 Popular Positions (ranked by submission count desc)
        var popularPositions = await _db.Positions
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
            .ToListAsync();

        // 4. Technology Tag Cloud (Frequency counts across vacancies and projects)
        var tagCloud = await _db.Tags
            .AsNoTracking()
            .Select(t => new TagCloudItemDto
            {
                Value = t.Name,
                Count = t.PositionTags.Count + t.ProjectTags.Count
            })
            .Where(t => t.Count > 0)
            .OrderByDescending(t => t.Count)
            .Take(30)
            .ToListAsync();

        return Ok(new DashboardResponseDto
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
        });
    }
}

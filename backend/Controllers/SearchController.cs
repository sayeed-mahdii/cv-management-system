using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models.DTOs;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<SearchController> _logger;

    public SearchController(AppDbContext db, ILogger<SearchController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Global Full-Text Search across positions, project descriptions, and candidate names.
    /// Utilizes PostgreSQL full-text search (tsvector + GIN indexes) with substring fallback.
    /// Open to guests and authenticated users.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GlobalSearchResponseDto>> Search([FromQuery] string? q, [FromQuery] string? type)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Ok(new GlobalSearchResponseDto
            {
                Query = q ?? string.Empty,
                TotalResults = 0,
                Results = new List<SearchResultItemDto>()
            });
        }

        var cleanQuery = q.Trim();
        var queryLower = cleanQuery.ToLowerInvariant();
        var results = new List<SearchResultItemDto>();

        var searchPositions = string.IsNullOrEmpty(type) || type.Equals("position", StringComparison.OrdinalIgnoreCase);
        var searchProjects = string.IsNullOrEmpty(type) || type.Equals("project", StringComparison.OrdinalIgnoreCase);
        var searchCandidates = string.IsNullOrEmpty(type) || type.Equals("candidate", StringComparison.OrdinalIgnoreCase);

        // 1. Search Positions (Title & ShortDescription with GIN index)
        if (searchPositions)
        {
            var matchedPositions = await _db.Positions
                .AsNoTracking()
                .Where(p =>
                    EF.Functions.ToTsVector("english", p.Title + " " + p.ShortDescription).Matches(EF.Functions.PlainToTsQuery("english", cleanQuery)) ||
                    p.Title.ToLower().Contains(queryLower) ||
                    p.ShortDescription.ToLower().Contains(queryLower))
                .OrderByDescending(p => p.UpdatedAt)
                .Take(20)
                .Select(p => new SearchResultItemDto
                {
                    Id = $"pos-{p.Id}",
                    EntityId = p.Id.ToString(),
                    Type = "Position",
                    Title = p.Title,
                    Subtitle = $"Vacancy · {p.Cvs.Count} applicant(s) · Max {p.MaxProjects} projects",
                    Snippet = p.ShortDescription,
                    Tags = p.PositionTags.Select(pt => pt.Tag!.Name).ToList(),
                    Url = "/positions",
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            results.AddRange(matchedPositions);
        }

        // 2. Search Projects (Name & Description with GIN index)
        if (searchProjects)
        {
            var matchedProjects = await _db.Projects
                .AsNoTracking()
                .Include(pr => pr.User)
                    .ThenInclude(u => u!.AttributeValues)
                .Where(pr =>
                    EF.Functions.ToTsVector("english", pr.Name + " " + pr.Description).Matches(EF.Functions.PlainToTsQuery("english", cleanQuery)) ||
                    pr.Name.ToLower().Contains(queryLower) ||
                    pr.Description.ToLower().Contains(queryLower))
                .OrderByDescending(pr => pr.StartDate)
                .Take(20)
                .Select(pr => new
                {
                    pr.Id,
                    pr.UserId,
                    pr.Name,
                    pr.Description,
                    Tags = pr.ProjectTags.Select(pt => pt.Tag!.Name).ToList(),
                    CandidateEmail = pr.User != null ? pr.User.Email : string.Empty,
                    FirstName = pr.User != null 
                        ? pr.User.AttributeValues.Where(v => v.AttributeDefinitionId == 1).Select(v => v.Value).FirstOrDefault()
                        : null,
                    LastName = pr.User != null 
                        ? pr.User.AttributeValues.Where(v => v.AttributeDefinitionId == 2).Select(v => v.Value).FirstOrDefault()
                        : null
                })
                .ToListAsync();

            foreach (var pr in matchedProjects)
            {
                var candidateName = $"{pr.FirstName ?? ""} {pr.LastName ?? ""}".Trim();
                if (string.IsNullOrWhiteSpace(candidateName))
                {
                    candidateName = pr.CandidateEmail;
                }

                results.Add(new SearchResultItemDto
                {
                    Id = $"proj-{pr.Id}",
                    EntityId = pr.Id.ToString(),
                    Type = "Project",
                    Title = pr.Name,
                    Subtitle = $"Portfolio Project · Author: {candidateName}",
                    Snippet = pr.Description.Length > 220 ? pr.Description.Substring(0, 220) + "..." : pr.Description,
                    Tags = pr.Tags,
                    Url = $"/profile/{pr.UserId}",
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // 3. Search Candidates (Name, Email, Location from UserAttributeValues)
        if (searchCandidates)
        {
            var matchedCandidates = await _db.Users
                .AsNoTracking()
                .Where(u =>
                    u.Email.ToLower().Contains(queryLower) ||
                    u.AttributeValues.Any(v => 
                        (v.AttributeDefinitionId == 1 || v.AttributeDefinitionId == 2 || v.AttributeDefinitionId == 3) &&
                        v.Value.ToLower().Contains(queryLower)))
                .Take(15)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.CreatedAt,
                    FirstName = u.AttributeValues.Where(v => v.AttributeDefinitionId == 1).Select(v => v.Value).FirstOrDefault(),
                    LastName = u.AttributeValues.Where(v => v.AttributeDefinitionId == 2).Select(v => v.Value).FirstOrDefault(),
                    Location = u.AttributeValues.Where(v => v.AttributeDefinitionId == 3).Select(v => v.Value).FirstOrDefault(),
                    ProjectsCount = u.Projects.Count,
                    Tags = u.Projects.SelectMany(p => p.ProjectTags).Select(pt => pt.Tag!.Name).Distinct().Take(5).ToList()
                })
                .ToListAsync();

            foreach (var c in matchedCandidates)
            {
                var fullName = $"{c.FirstName ?? ""} {c.LastName ?? ""}".Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = c.Email;
                }

                results.Add(new SearchResultItemDto
                {
                    Id = $"cand-{c.Id}",
                    EntityId = c.Id,
                    Type = "Candidate",
                    Title = fullName,
                    Subtitle = $"Candidate Profile · {c.Location ?? "Location not specified"} · {c.Email}",
                    Snippet = $"Candidate profile featuring {c.ProjectsCount} portfolio project(s).",
                    Tags = c.Tags,
                    Url = $"/profile/{c.Id}",
                    UpdatedAt = c.CreatedAt
                });
            }
        }

        return Ok(new GlobalSearchResponseDto
        {
            Query = cleanQuery,
            TotalResults = results.Count,
            Results = results
        });
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models.DTOs;
using backend.Models.Entities;
using backend.Models.Enums;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(AppDbContext db, ILogger<PositionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string GetCurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? string.Empty;

    /// <summary>
    /// Lists all positions with paged results and keyword/tag filtering.
    /// Direct projection without SELECT * scans. Public read access.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PositionSummaryDto>>> GetPositions([FromQuery] PositionSearchQuery query)
    {
        var dbQuery = _db.Positions
            .AsNoTracking()
            .AsQueryable();

        // 1. Keyword search (Title or ShortDescription)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(p =>
                p.Title.ToLower().Contains(search) ||
                p.ShortDescription.ToLower().Contains(search));
        }

        // 2. Tag filter
        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            var tagNorm = query.Tag.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(p => p.PositionTags.Any(pt => pt.Tag!.NormalizedName == tagNorm));
        }

        // Order by latest updated first
        dbQuery = dbQuery.OrderByDescending(p => p.UpdatedAt);

        var totalCount = await dbQuery.CountAsync();
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : (query.PageSize > 100 ? 100 : query.PageSize);

        var items = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return Ok(new PagedResult<PositionSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Returns details of a specific position, including required attributes and tags.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PositionDetailDto>> GetPosition(int id)
    {
        var position = await _db.Positions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PositionDetailDto
            {
                Id = p.Id,
                Title = p.Title,
                ShortDescription = p.ShortDescription,
                IsPublic = p.IsPublic,
                MaxProjects = p.MaxProjects,
                Version = p.Version,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ApplicationsCount = p.Cvs.Count,
                Tags = p.PositionTags.Select(pt => pt.Tag!.Name).ToList(),
                Attributes = p.PositionAttributes
                    .OrderBy(pa => pa.SortOrder)
                    .Select(pa => new PositionRequiredAttributeDto
                    {
                        AttributeDefinitionId = pa.AttributeDefinitionId,
                        Name = pa.AttributeDefinition!.Name,
                        DataType = pa.AttributeDefinition.DataType.ToString(),
                        CategoryName = pa.AttributeDefinition.Category != null ? pa.AttributeDefinition.Category.Name : string.Empty,
                        SortOrder = pa.SortOrder,
                        Options = pa.AttributeDefinition.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (position == null)
        {
            return NotFound(new { message = $"Position with ID {id} not found." });
        }

        // Check if currently authenticated candidate has already applied
        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            position.HasApplied = await _db.Cvs.AnyAsync(c => c.PositionId == id && c.UserId == userId);
        }

        return Ok(position);
    }

    /// <summary>
    /// Creates a new Position template (Spec §4.4).
    /// Restricted to Recruiters and Administrators.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<PositionDetailDto>> CreatePosition([FromBody] CreatePositionDto dto)
    {
        var position = new Position
        {
            Title = dto.Title.Trim(),
            ShortDescription = dto.ShortDescription.Trim(),
            MaxProjects = Math.Clamp(dto.MaxProjects, 1, 10),
            IsPublic = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 1. Link Position Attributes
        if (dto.AttributeIds != null && dto.AttributeIds.Count > 0)
        {
            var validAttrIds = await _db.AttributeDefinitions
                .Where(a => dto.AttributeIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            var validSet = validAttrIds.ToHashSet();
            var ordered = dto.AttributeIds.Where(id => validSet.Contains(id)).Distinct().ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                position.PositionAttributes.Add(new PositionAttribute
                {
                    AttributeDefinitionId = ordered[i],
                    SortOrder = i + 1
                });
            }
        }

        // 2. Link Position Tags (Created in DB on first use)
        if (dto.Tags != null && dto.Tags.Count > 0)
        {
            var cleanedTags = dto.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var normalizedTags = cleanedTags.Select(t => t.ToLowerInvariant()).ToList();
            var existingTags = await _db.Tags
                .Where(t => normalizedTags.Contains(t.NormalizedName))
                .ToListAsync();

            var tagMap = existingTags.ToDictionary(t => t.NormalizedName);

            foreach (var tagStr in cleanedTags)
            {
                var norm = tagStr.ToLowerInvariant();
                if (!tagMap.TryGetValue(norm, out var tagEntity))
                {
                    tagEntity = new Tag { Name = tagStr, NormalizedName = norm };
                    _db.Tags.Add(tagEntity);
                    tagMap[norm] = tagEntity;
                }

                position.PositionTags.Add(new PositionTag
                {
                    Position = position,
                    Tag = tagEntity
                });
            }
        }

        _db.Positions.Add(position);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created new position: '{Title}' (ID: {Id})", position.Title, position.Id);
        return CreatedAtAction(nameof(GetPosition), new { id = position.Id }, await GetPositionDetailInternal(position.Id));
    }

    /// <summary>
    /// Updates a Position with Optimistic Concurrency check.
    /// Updates UpdatedAt timestamp for "Latest Positions" sorting.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<PositionDetailDto>> UpdatePosition(int id, [FromBody] UpdatePositionDto dto)
    {
        var position = await _db.Positions
            .Include(p => p.PositionAttributes)
            .Include(p => p.PositionTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
        {
            return NotFound(new { message = $"Position with ID {id} not found." });
        }

        // Concurrency check
        if (position.Version != dto.Version)
        {
            return Conflict(new
            {
                message = "This position was modified by another user. Please reload the latest changes.",
                currentVersion = position.Version
            });
        }

        position.Title = dto.Title.Trim();
        position.ShortDescription = dto.ShortDescription.Trim();
        position.MaxProjects = Math.Clamp(dto.MaxProjects, 1, 10);
        position.Version++;
        position.UpdatedAt = DateTime.UtcNow;

        // 1. Reconcile Attributes
        var targetAttrIds = new List<int>();
        if (dto.AttributeIds != null && dto.AttributeIds.Count > 0)
        {
            var validAttrIds = await _db.AttributeDefinitions
                .Where(a => dto.AttributeIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            var validSet = validAttrIds.ToHashSet();
            targetAttrIds = dto.AttributeIds.Where(id => validSet.Contains(id)).Distinct().ToList();
        }

        var targetAttrSet = targetAttrIds.ToHashSet();
        var attrsToRemove = position.PositionAttributes
            .Where(pa => !targetAttrSet.Contains(pa.AttributeDefinitionId))
            .ToList();
        foreach (var attr in attrsToRemove)
        {
            position.PositionAttributes.Remove(attr);
        }

        for (int i = 0; i < targetAttrIds.Count; i++)
        {
            var attrId = targetAttrIds[i];
            var existing = position.PositionAttributes.FirstOrDefault(pa => pa.AttributeDefinitionId == attrId);
            if (existing != null)
            {
                existing.SortOrder = i + 1;
            }
            else
            {
                position.PositionAttributes.Add(new PositionAttribute
                {
                    PositionId = position.Id,
                    AttributeDefinitionId = attrId,
                    SortOrder = i + 1
                });
            }
        }

        // 2. Reconcile Tags
        var cleanedTags = (dto.Tags ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var normalizedTags = cleanedTags.Select(t => t.ToLowerInvariant()).ToList();
        var targetTagNormSet = normalizedTags.ToHashSet();

        var tagsToRemove = position.PositionTags
            .Where(pt => pt.Tag == null || !targetTagNormSet.Contains(pt.Tag.NormalizedName))
            .ToList();
        foreach (var pt in tagsToRemove)
        {
            position.PositionTags.Remove(pt);
        }

        if (cleanedTags.Count > 0)
        {
            var existingTags = await _db.Tags
                .Where(t => normalizedTags.Contains(t.NormalizedName))
                .ToListAsync();

            var tagMap = existingTags.ToDictionary(t => t.NormalizedName);
            var currentTagNorms = position.PositionTags
                .Where(pt => pt.Tag != null)
                .Select(pt => pt.Tag!.NormalizedName)
                .ToHashSet();

            foreach (var tagStr in cleanedTags)
            {
                var norm = tagStr.ToLowerInvariant();
                if (currentTagNorms.Contains(norm))
                {
                    continue;
                }

                if (!tagMap.TryGetValue(norm, out var tagEntity))
                {
                    tagEntity = new Tag { Name = tagStr, NormalizedName = norm };
                    _db.Tags.Add(tagEntity);
                    tagMap[norm] = tagEntity;
                }

                position.PositionTags.Add(new PositionTag
                {
                    PositionId = position.Id,
                    Tag = tagEntity
                });
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Concurrency conflict: Position was modified concurrently." });
        }

        return Ok(await GetPositionDetailInternal(position.Id));
    }

    /// <summary>
    /// Deletes a position. Cascades to attributes, tags, access rules, and CVs.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<IActionResult> DeletePosition(int id)
    {
        var position = await _db.Positions.FindAsync(id);
        if (position == null)
        {
            return NotFound();
        }

        _db.Positions.Remove(position);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted position ID: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Duplicates an existing position into a new record with Version 1 and 0 applications.
    /// </summary>
    [HttpPost("{id:int}/duplicate")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<PositionDetailDto>> DuplicatePosition(int id)
    {
        var original = await _db.Positions
            .Include(p => p.PositionAttributes)
            .Include(p => p.PositionTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (original == null)
        {
            return NotFound(new { message = $"Position with ID {id} not found." });
        }

        var clone = new Position
        {
            Title = $"{original.Title} (Copy)",
            ShortDescription = original.ShortDescription,
            MaxProjects = original.MaxProjects,
            IsPublic = original.IsPublic,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var attr in original.PositionAttributes)
        {
            clone.PositionAttributes.Add(new PositionAttribute
            {
                AttributeDefinitionId = attr.AttributeDefinitionId,
                SortOrder = attr.SortOrder
            });
        }

        foreach (var pt in original.PositionTags)
        {
            clone.PositionTags.Add(new PositionTag
            {
                TagId = pt.TagId
            });
        }

        _db.Positions.Add(clone);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPosition), new { id = clone.Id }, await GetPositionDetailInternal(clone.Id));
    }

    /// <summary>
    /// Candidate applies to a position with 1 click.
    /// Enforces strictly at most one application per candidate per position.
    /// </summary>
    [HttpPost("{id:int}/apply")]
    [Authorize(Roles = "Candidate,Administrator")]
    public async Task<IActionResult> ApplyToPosition(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var positionExists = await _db.Positions.AnyAsync(p => p.Id == id);
        if (!positionExists)
        {
            return NotFound(new { message = $"Position with ID {id} not found." });
        }

        // Enforce unique application constraint
        var alreadyApplied = await _db.Cvs.AnyAsync(c => c.PositionId == id && c.UserId == userId);
        if (alreadyApplied)
        {
            return BadRequest(new { message = "You have already submitted an application for this position." });
        }

        var cv = new Cv
        {
            UserId = userId,
            PositionId = id,
            State = CvState.Published,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Cvs.Add(cv);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Candidate {UserId} applied to position {PositionId}", userId, id);
        return Ok(new { message = "Application submitted successfully!", cvId = cv.Id });
    }

    /// <summary>
    /// Candidate withdraws their application from a position (Spec §4.2).
    /// </summary>
    [HttpDelete("{id:int}/apply")]
    [Authorize(Roles = "Candidate,Administrator")]
    public async Task<IActionResult> WithdrawApplicationForPosition(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var cv = await _db.Cvs.FirstOrDefaultAsync(c => c.PositionId == id && c.UserId == userId);
        if (cv == null)
        {
            return NotFound(new { message = "You do not have an active application for this position." });
        }

        _db.Cvs.Remove(cv);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Candidate {UserId} withdrew application for position {PositionId}", userId, id);
        return NoContent();
    }

    /// <summary>
    /// Returns the list of applicants for a specific position (Spec §4.2).
    /// Restricted to Recruiters and Administrators.
    /// Direct projection to avoid N+1 queries.
    /// </summary>
    [HttpGet("{id:int}/applicants")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<List<ApplicantSummaryDto>>> GetApplicants(int id)
    {
        var positionExists = await _db.Positions.AnyAsync(p => p.Id == id);
        if (!positionExists)
        {
            return NotFound(new { message = $"Position with ID {id} not found." });
        }

        var applicants = await _db.Cvs
            .AsNoTracking()
            .Where(c => c.PositionId == id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.UserId,
                Email = c.User != null ? c.User.Email : string.Empty,
                FirstName = c.User != null 
                    ? c.User.AttributeValues.Where(v => v.AttributeDefinitionId == 1).Select(v => v.Value).FirstOrDefault()
                    : null,
                LastName = c.User != null 
                    ? c.User.AttributeValues.Where(v => v.AttributeDefinitionId == 2).Select(v => v.Value).FirstOrDefault()
                    : null,
                Location = c.User != null 
                    ? c.User.AttributeValues.Where(v => v.AttributeDefinitionId == 3).Select(v => v.Value).FirstOrDefault()
                    : null,
                PhotoUrl = c.User != null 
                    ? c.User.AttributeValues.Where(v => v.AttributeDefinitionId == 4).Select(v => v.Value).FirstOrDefault()
                    : null,
                State = c.State.ToString(),
                c.CreatedAt,
                c.Version
            })
            .ToListAsync();

        var result = applicants.Select(a =>
        {
            var fullName = $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = a.Email;
            }

            return new ApplicantSummaryDto
            {
                CvId = a.Id,
                CandidateId = a.UserId,
                CandidateEmail = a.Email,
                CandidateName = fullName,
                Location = a.Location ?? "Not specified",
                PhotoUrl = a.PhotoUrl,
                State = a.State,
                SubmittedAt = a.CreatedAt,
                Version = a.Version
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Inspects a candidate's virtual profile for a position (Killer Feature #3).
    /// Dynamically filters projects by the position's technology tags and limits to MaxProjects.
    /// Restricted to Recruiters and Administrators.
    /// </summary>
    [HttpGet("{id:int}/applicants/{candidateId}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<ApplicantDetailDto>> GetApplicantDetail(int id, string candidateId)
    {
        var position = await _db.Positions
            .AsNoTracking()
            .Include(p => p.PositionTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PositionAttributes)
                .ThenInclude(pa => pa.AttributeDefinition)
                    .ThenInclude(ad => ad!.Category)
            .Include(p => p.PositionAttributes)
                .ThenInclude(pa => pa.AttributeDefinition)
                    .ThenInclude(ad => ad!.Options)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
        {
            return NotFound(new { message = $"Position with ID {id} not found." });
        }

        var cv = await _db.Cvs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PositionId == id && c.UserId == candidateId);

        if (cv == null)
        {
            return NotFound(new { message = "Candidate application not found for this position." });
        }

        var candidate = await _db.Users
            .AsNoTracking()
            .Include(u => u.AttributeValues)
                .ThenInclude(av => av.AttributeDefinition)
                    .ThenInclude(ad => ad!.Category)
            .Include(u => u.AttributeValues)
                .ThenInclude(av => av.AttributeDefinition)
                    .ThenInclude(ad => ad!.Options)
            .FirstOrDefaultAsync(u => u.Id == candidateId);

        if (candidate == null)
        {
            return NotFound(new { message = $"Candidate with ID {candidateId} not found." });
        }

        // 1. Separate Me attributes vs Info attributes
        var meAttrs = new List<UserAttributeValueDto>();
        var infoAttrs = new List<UserAttributeValueDto>();
        var userAttrMap = new Dictionary<int, UserAttributeValue>();

        foreach (var av in candidate.AttributeValues)
        {
            userAttrMap[av.AttributeDefinitionId] = av;
            var def = av.AttributeDefinition;
            if (def == null) continue;

            var dto = new UserAttributeValueDto
            {
                AttributeDefinitionId = def.Id,
                Name = def.Name,
                DataType = def.DataType.ToString(),
                CategoryName = def.Category?.Name ?? string.Empty,
                Value = av.Value,
                ImagePublicId = av.ImagePublicId,
                IsBuiltIn = def.IsBuiltIn,
                Options = def.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
            };

            if (def.IsBuiltIn)
            {
                meAttrs.Add(dto);
            }
            else
            {
                infoAttrs.Add(dto);
            }
        }

        // 2. Check Required Attributes
        var requiredCheck = new List<ApplicantAttributeMatchDto>();
        foreach (var pa in position.PositionAttributes.OrderBy(pa => pa.SortOrder))
        {
            var def = pa.AttributeDefinition;
            if (def == null) continue;

            userAttrMap.TryGetValue(def.Id, out var candidateVal);
            var valStr = candidateVal?.Value ?? string.Empty;

            requiredCheck.Add(new ApplicantAttributeMatchDto
            {
                AttributeDefinitionId = def.Id,
                Name = def.Name,
                DataType = def.DataType.ToString(),
                CategoryName = def.Category?.Name ?? string.Empty,
                CandidateValue = valStr,
                IsSatisfied = !string.IsNullOrWhiteSpace(valStr)
            });
        }

        // 3. Assemble and Filter Portfolio Projects (Killer Feature #3)
        // Position technology tags filter candidate projects; capped at position.MaxProjects
        var allProjects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.UserId == candidateId)
            .OrderByDescending(p => p.StartDate)
            .Include(p => p.ProjectTags)
                .ThenInclude(pt => pt.Tag)
            .ToListAsync();

        var positionTagNorms = position.PositionTags
            .Where(pt => pt.Tag != null)
            .Select(pt => pt.Tag!.NormalizedName)
            .ToHashSet();

        List<Project> selectedProjects;
        if (positionTagNorms.Count > 0)
        {
            var matching = allProjects
                .Where(p => p.ProjectTags.Any(pt => pt.Tag != null && positionTagNorms.Contains(pt.Tag.NormalizedName)))
                .ToList();

            if (matching.Count > 0)
            {
                selectedProjects = matching.Take(position.MaxProjects).ToList();
            }
            else
            {
                selectedProjects = allProjects.Take(position.MaxProjects).ToList();
            }
        }
        else
        {
            selectedProjects = allProjects.Take(position.MaxProjects).ToList();
        }

        var projectDtos = selectedProjects.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            StartDate = p.StartDate.ToString("yyyy-MM-dd"),
            EndDate = p.EndDate?.ToString("yyyy-MM-dd"),
            Description = p.Description,
            Version = p.Version,
            Tags = p.ProjectTags.Select(pt => pt.Tag?.Name ?? string.Empty).ToList()
        }).ToList();

        var firstName = candidate.AttributeValues.FirstOrDefault(v => v.AttributeDefinitionId == 1)?.Value ?? "";
        var lastName = candidate.AttributeValues.FirstOrDefault(v => v.AttributeDefinitionId == 2)?.Value ?? "";
        var candidateFullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(candidateFullName))
        {
            candidateFullName = candidate.Email;
        }

        return Ok(new ApplicantDetailDto
        {
            CvId = cv.Id,
            PositionId = position.Id,
            PositionTitle = position.Title,
            MaxProjects = position.MaxProjects,
            PositionTags = position.PositionTags.Select(pt => pt.Tag!.Name).ToList(),
            CandidateId = candidate.Id,
            CandidateEmail = candidate.Email,
            CandidateName = candidateFullName,
            SubmittedAt = cv.CreatedAt,
            State = cv.State.ToString(),
            MeAttributes = meAttrs,
            InfoAttributes = infoAttrs,
            RequiredAttributesCheck = requiredCheck,
            Projects = projectDtos
        });
    }

    /// <summary>
    /// Internal helper to assemble PositionDetailDto.
    /// </summary>
    private async Task<PositionDetailDto> GetPositionDetailInternal(int id)
    {
        return await _db.Positions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PositionDetailDto
            {
                Id = p.Id,
                Title = p.Title,
                ShortDescription = p.ShortDescription,
                IsPublic = p.IsPublic,
                MaxProjects = p.MaxProjects,
                Version = p.Version,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ApplicationsCount = p.Cvs.Count,
                Tags = p.PositionTags.Select(pt => pt.Tag!.Name).ToList(),
                Attributes = p.PositionAttributes
                    .OrderBy(pa => pa.SortOrder)
                    .Select(pa => new PositionRequiredAttributeDto
                    {
                        AttributeDefinitionId = pa.AttributeDefinitionId,
                        Name = pa.AttributeDefinition!.Name,
                        DataType = pa.AttributeDefinition.DataType.ToString(),
                        CategoryName = pa.AttributeDefinition.Category != null ? pa.AttributeDefinition.Category.Name : string.Empty,
                        SortOrder = pa.SortOrder,
                        Options = pa.AttributeDefinition.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
                    })
                    .ToList()
            })
            .FirstAsync();
    }
}

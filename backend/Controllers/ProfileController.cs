using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models.DTOs;
using backend.Models.Entities;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CloudinaryService _cloudinary;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(AppDbContext db, CloudinaryService cloudinary, ILogger<ProfileController> logger)
    {
        _db = db;
        _cloudinary = cloudinary;
        _logger = logger;
    }

    /// <summary>
    /// Helper to get current authenticated user ID from ClaimsPrincipal.
    /// </summary>
    private string GetCurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? string.Empty;

    /// <summary>
    /// Helper to verify if the caller is the resource owner or an administrator.
    /// </summary>
    private bool IsOwnerOrAdmin(string userId) =>
        GetCurrentUserId() == userId || User.IsInRole("Administrator");

    /// <summary>
    /// Loads a candidate profile with Me attributes, Info attributes, Projects, and Applications.
    /// Uses batch queries to avoid N+1 database queries.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<ProfileResponseDto>> GetProfile(string userId)
    {
        var callerId = GetCurrentUserId();
        var isRecruiterOrAdmin = User.IsInRole("Recruiter") || User.IsInRole("Administrator");

        // Candidates can only view their own profile; Recruiters & Admins can view any profile
        if (callerId != userId && !isRecruiterOrAdmin)
        {
            return Forbid();
        }

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{userId}' not found." });
        }

        // 1. Load all built-in definitions (to ensure First Name, Last Name, Location, Photo always appear)
        var builtInDefs = await _db.AttributeDefinitions
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Options)
            .Where(a => a.IsBuiltIn)
            .OrderBy(a => a.Id)
            .ToListAsync();

        // 2. Load all existing values for this user
        var userValues = await _db.UserAttributeValues
            .AsNoTracking()
            .Include(v => v.AttributeDefinition)
                .ThenInclude(d => d!.Category)
            .Include(v => v.AttributeDefinition)
                .ThenInclude(d => d!.Options)
            .Where(v => v.UserId == userId)
            .ToListAsync();

        var valueMap = userValues.ToDictionary(v => v.AttributeDefinitionId);

        // 3. Assemble MeAttributes (mandatory built-in attributes)
        var meAttributes = builtInDefs.Select(def =>
        {
            valueMap.TryGetValue(def.Id, out var val);
            return new UserAttributeValueDto
            {
                AttributeDefinitionId = def.Id,
                Name = def.Name,
                DataType = def.DataType.ToString(),
                CategoryName = def.Category?.Name ?? "Profile",
                Value = val?.Value ?? string.Empty,
                ImagePublicId = val?.ImagePublicId,
                IsBuiltIn = true,
                Options = def.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
            };
        }).ToList();

        // 4. Assemble InfoAttributes (custom library attributes selected by the user)
        var infoAttributes = userValues
            .Where(v => v.AttributeDefinition != null && !v.AttributeDefinition.IsBuiltIn)
            .OrderBy(v => v.AttributeDefinition!.Name)
            .Select(v => new UserAttributeValueDto
            {
                AttributeDefinitionId = v.AttributeDefinitionId,
                Name = v.AttributeDefinition!.Name,
                DataType = v.AttributeDefinition.DataType.ToString(),
                CategoryName = v.AttributeDefinition.Category?.Name ?? "General",
                Value = v.Value,
                ImagePublicId = v.ImagePublicId,
                IsBuiltIn = false,
                Options = v.AttributeDefinition.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
            }).ToList();

        // 5. Load Projects with Tags in a single query
        var projects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate.ToString("yyyy-MM-dd"),
                EndDate = p.EndDate.HasValue ? p.EndDate.Value.ToString("yyyy-MM-dd") : null,
                Description = p.Description,
                Version = p.Version,
                Tags = p.ProjectTags.Select(pt => pt.Tag!.Name).ToList()
            })
            .ToListAsync();

        // 6. Load CV Applications in a single query
        var applications = await _db.Cvs
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ApplicationSummaryDto
            {
                CvId = c.Id,
                PositionId = c.PositionId,
                PositionTitle = c.Position != null ? c.Position.Title : "Unknown Position",
                State = c.State.ToString(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(new ProfileResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Version = user.Version,
            PreferredLanguage = user.PreferredLanguage,
            PreferredTheme = user.PreferredTheme,
            IsBlocked = user.IsBlocked,
            Roles = user.Roles.Select(r => r.Role).ToList(),
            MeAttributes = meAttributes,
            InfoAttributes = infoAttributes,
            Projects = projects,
            Applications = applications
        });
    }

    /// <summary>
    /// Alias for GetProfile using currently logged in user ID.
    /// </summary>
    [HttpGet("me")]
    public Task<ActionResult<ProfileResponseDto>> GetMyProfile() =>
        GetProfile(GetCurrentUserId());

    /// <summary>
    /// Batch saves candidate attribute values with Optimistic Concurrency check.
    /// Batch updates values in memory; avoids database queries inside loops.
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateProfile(string userId, [FromBody] UpdateProfileDto dto)
    {
        if (!IsOwnerOrAdmin(userId))
        {
            return Forbid();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{userId}' not found." });
        }

        // Optimistic Locking Concurrency Check
        if (user.Version != dto.Version)
        {
            return Conflict(new
            {
                message = "Your profile was modified in another window. Please reload to see the latest changes.",
                currentVersion = user.Version
            });
        }

        // 1. Load all existing attribute values for this user in ONE query
        var existingValues = await _db.UserAttributeValues
            .Where(v => v.UserId == userId)
            .ToListAsync();

        var existingMap = existingValues.ToDictionary(v => v.AttributeDefinitionId);

        // 2. Batch update / insert in memory
        foreach (var attrInput in dto.Attributes)
        {
            if (existingMap.TryGetValue(attrInput.AttributeDefinitionId, out var existing))
            {
                existing.Value = attrInput.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(attrInput.ImagePublicId))
                {
                    existing.ImagePublicId = attrInput.ImagePublicId;
                }
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.UserAttributeValues.Add(new UserAttributeValue
                {
                    UserId = userId,
                    AttributeDefinitionId = attrInput.AttributeDefinitionId,
                    Value = attrInput.Value ?? string.Empty,
                    ImagePublicId = attrInput.ImagePublicId,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // 3. Increment Version concurrency token
        user.Version++;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Concurrency conflict: Profile was modified concurrently." });
        }

        return Ok(new
        {
            message = "Profile updated successfully.",
            version = user.Version
        });
    }

    /// <summary>
    /// Removes a custom attribute value from a candidate's profile.
    /// </summary>
    [HttpDelete("{userId}/attributes/{attributeDefinitionId:int}")]
    public async Task<IActionResult> RemoveAttributeValue(string userId, int attributeDefinitionId)
    {
        if (!IsOwnerOrAdmin(userId))
        {
            return Forbid();
        }

        var val = await _db.UserAttributeValues
            .Include(v => v.AttributeDefinition)
            .FirstOrDefaultAsync(v => v.UserId == userId && v.AttributeDefinitionId == attributeDefinitionId);

        if (val == null)
        {
            return NotFound();
        }

        if (val.AttributeDefinition?.IsBuiltIn == true)
        {
            return BadRequest(new { message = "Built-in profile attributes cannot be removed." });
        }

        _db.UserAttributeValues.Remove(val);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Adds a project to candidate's portfolio. Tags are created in the database on first use.
    /// </summary>
    [HttpPost("{userId}/projects")]
    public async Task<ActionResult<ProjectDto>> AddProject(string userId, [FromBody] CreateProjectDto dto)
    {
        if (!IsOwnerOrAdmin(userId))
        {
            return Forbid();
        }

        var project = new Project
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Description = dto.Description?.Trim() ?? string.Empty,
            Version = 1
        };

        // Process Tags (Find existing or create on first use)
        if (dto.Tags != null && dto.Tags.Count > 0)
        {
            var cleanedTags = dto.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var normalizedTags = cleanedTags.Select(t => t.ToLowerInvariant()).ToList();

            var existingDbTags = await _db.Tags
                .Where(t => normalizedTags.Contains(t.NormalizedName))
                .ToListAsync();

            var existingMap = existingDbTags.ToDictionary(t => t.NormalizedName);

            foreach (var tagName in cleanedTags)
            {
                var norm = tagName.ToLowerInvariant();
                if (!existingMap.TryGetValue(norm, out var tagEntity))
                {
                    tagEntity = new Tag
                    {
                        Name = tagName,
                        NormalizedName = norm
                    };
                    _db.Tags.Add(tagEntity);
                    existingMap[norm] = tagEntity;
                }

                project.ProjectTags.Add(new ProjectTag
                {
                    Project = project,
                    Tag = tagEntity
                });
            }
        }

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfile), new { userId }, new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            StartDate = project.StartDate.ToString("yyyy-MM-dd"),
            EndDate = project.EndDate?.ToString("yyyy-MM-dd"),
            Description = project.Description,
            Version = project.Version,
            Tags = project.ProjectTags.Select(pt => pt.Tag?.Name ?? string.Empty).ToList()
        });
    }

    /// <summary>
    /// Updates a project with Optimistic Concurrency check.
    /// </summary>
    [HttpPut("{userId}/projects/{projectId:int}")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(string userId, int projectId, [FromBody] UpdateProjectDto dto)
    {
        if (!IsOwnerOrAdmin(userId))
        {
            return Forbid();
        }

        var project = await _db.Projects
            .Include(p => p.ProjectTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

        if (project == null)
        {
            return NotFound(new { message = $"Project with ID {projectId} not found." });
        }

        // Optimistic locking concurrency check
        if (project.Version != dto.Version)
        {
            return Conflict(new { message = "This project was modified in another window. Please reload." });
        }

        project.Name = dto.Name.Trim();
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.Description = dto.Description?.Trim() ?? string.Empty;
        project.Version++;

        // Update Tags
        _db.ProjectTags.RemoveRange(project.ProjectTags);

        if (dto.Tags != null && dto.Tags.Count > 0)
        {
            var cleanedTags = dto.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var normalizedTags = cleanedTags.Select(t => t.ToLowerInvariant()).ToList();
            var existingDbTags = await _db.Tags
                .Where(t => normalizedTags.Contains(t.NormalizedName))
                .ToListAsync();

            var existingMap = existingDbTags.ToDictionary(t => t.NormalizedName);

            foreach (var tagName in cleanedTags)
            {
                var norm = tagName.ToLowerInvariant();
                if (!existingMap.TryGetValue(norm, out var tagEntity))
                {
                    tagEntity = new Tag
                    {
                        Name = tagName,
                        NormalizedName = norm
                    };
                    _db.Tags.Add(tagEntity);
                    existingMap[norm] = tagEntity;
                }

                project.ProjectTags.Add(new ProjectTag
                {
                    ProjectId = project.Id,
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
            return Conflict(new { message = "Concurrency conflict: Project was modified concurrently." });
        }

        return Ok(new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            StartDate = project.StartDate.ToString("yyyy-MM-dd"),
            EndDate = project.EndDate?.ToString("yyyy-MM-dd"),
            Description = project.Description,
            Version = project.Version,
            Tags = project.ProjectTags.Select(pt => pt.Tag?.Name ?? string.Empty).ToList()
        });
    }

    /// <summary>
    /// Deletes a project from candidate's portfolio.
    /// </summary>
    [HttpDelete("{userId}/projects/{projectId:int}")]
    public async Task<IActionResult> DeleteProject(string userId, int projectId)
    {
        if (!IsOwnerOrAdmin(userId))
        {
            return Forbid();
        }

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null)
        {
            return NotFound();
        }

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Generates Cloudinary signed upload parameters so the browser uploads directly to the CDN.
    /// The API secret never leaves the server.
    /// </summary>
    [HttpGet("upload-signature")]
    public ActionResult<CloudinarySignatureDto> GetUploadSignature()
    {
        var sig = _cloudinary.GenerateSignedUploadParams("recruitov/avatars");
        return Ok(sig);
    }

    /// <summary>
    /// Candidate withdraws their application.
    /// </summary>
    [HttpDelete("applications/{cvId:int}")]
    public async Task<IActionResult> WithdrawApplication(int cvId)
    {
        var callerId = GetCurrentUserId();
        var cv = await _db.Cvs.FirstOrDefaultAsync(c => c.Id == cvId);

        if (cv == null)
        {
            return NotFound(new { message = $"Application with ID {cvId} not found." });
        }

        if (cv.UserId != callerId && !User.IsInRole("Administrator"))
        {
            return Forbid();
        }

        _db.Cvs.Remove(cv);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Application {CvId} withdrawn by user {UserId}", cvId, callerId);
        return NoContent();
    }
}

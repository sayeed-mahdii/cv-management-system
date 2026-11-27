using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Recruitov.Application.Core.Helpers;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;

namespace Recruitov.Infrastructure.Data.DataServices;

public class ProfileDataService(AppDbContext db) : IProfileDataService
{
    public async Task<ProfileResponseDto?> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return null;

        var builtInDefs = await db.AttributeDefinitions
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Options)
            .Where(a => a.IsBuiltIn)
            .OrderBy(a => a.Id)
            .ToListAsync(ct);

        var userValues = await db.UserAttributeValues
            .AsNoTracking()
            .Include(v => v.AttributeDefinition).ThenInclude(d => d!.Category)
            .Include(v => v.AttributeDefinition).ThenInclude(d => d!.Options)
            .Where(v => v.UserId == userId)
            .ToListAsync(ct);

        var valueMap = userValues.ToDictionary(v => v.AttributeDefinitionId);

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

        var projects = await db.Projects
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
            .ToListAsync(ct);

        var applications = await db.Cvs
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
            .ToListAsync(ct);

        return new ProfileResponseDto
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
        };
    }

    public async Task<ErrorOr<int>> UpdateProfileAsync(string userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Error.NotFound("Profile.NotFound", $"User with ID '{userId}' not found.");

        if (user.Version != dto.Version)
        {
            return Error.Conflict(
                "Profile.Concurrency",
                "Your profile was modified in another window. Please reload to see the latest changes.",
                new Dictionary<string, object> { ["currentVersion"] = user.Version });
        }

        var existingValues = await db.UserAttributeValues.Where(v => v.UserId == userId).ToListAsync(ct);
        var existingMap = existingValues.ToDictionary(v => v.AttributeDefinitionId);

        foreach (var attrInput in dto.Attributes)
        {
            if (existingMap.TryGetValue(attrInput.AttributeDefinitionId, out var existing))
            {
                existing.Value = attrInput.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(attrInput.ImagePublicId))
                    existing.ImagePublicId = attrInput.ImagePublicId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.UserAttributeValues.Add(new UserAttributeValue
                {
                    UserId = userId,
                    AttributeDefinitionId = attrInput.AttributeDefinitionId,
                    Value = attrInput.Value ?? string.Empty,
                    ImagePublicId = attrInput.ImagePublicId,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        user.Version++;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict("Profile.Concurrency", "Concurrency conflict: Profile was modified concurrently.");
        }

        return user.Version;
    }

    public async Task<ErrorOr<Deleted>> RemoveAttributeValueAsync(string userId, int attributeDefinitionId, CancellationToken ct = default)
    {
        var val = await db.UserAttributeValues
            .Include(v => v.AttributeDefinition)
            .FirstOrDefaultAsync(v => v.UserId == userId && v.AttributeDefinitionId == attributeDefinitionId, ct);

        if (val is null)
            return Error.NotFound("Profile.AttributeNotFound", "Attribute value not found.");

        if (val.AttributeDefinition?.IsBuiltIn == true)
            return Error.Validation("Profile.BuiltInAttribute", "Built-in profile attributes cannot be removed.");

        db.UserAttributeValues.Remove(val);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ProjectDto> AddProjectAsync(string userId, CreateProjectDto dto, CancellationToken ct = default)
    {
        var project = new Project
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Description = dto.Description?.Trim() ?? string.Empty,
            Version = 1
        };

        await AttachTagsAsync(project, dto.Tags, isNewProject: true, ct);

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        return MapProjectDto(project);
    }

    public async Task<ErrorOr<ProjectDto>> UpdateProjectAsync(string userId, int projectId, UpdateProjectDto dto, CancellationToken ct = default)
    {
        var project = await db.Projects
            .Include(p => p.ProjectTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);

        if (project is null)
            return Error.NotFound("Project.NotFound", $"Project with ID {projectId} not found.");

        if (project.Version != dto.Version)
            return Error.Conflict("Project.Concurrency", "This project was modified in another window. Please reload.");

        project.Name = dto.Name.Trim();
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.Description = dto.Description?.Trim() ?? string.Empty;
        project.Version++;

        db.ProjectTags.RemoveRange(project.ProjectTags);
        project.ProjectTags.Clear();

        await AttachTagsAsync(project, dto.Tags, isNewProject: false, ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict("Project.Concurrency", "Concurrency conflict: Project was modified concurrently.");
        }

        return MapProjectDto(project);
    }

    private async Task AttachTagsAsync(Project project, List<string>? rawTags, bool isNewProject, CancellationToken ct)
    {
        var normalizedTags = TagNormalizationHelper.NormalizeTags(rawTags);
        if (normalizedTags.Count == 0) return;

        var normNames = normalizedTags.Select(t => t.NormalizedName).ToList();
        var existingDbTags = await db.Tags.Where(t => normNames.Contains(t.NormalizedName)).ToListAsync(ct);
        var existingMap = existingDbTags.ToDictionary(t => t.NormalizedName);

        foreach (var tag in normalizedTags)
        {
            if (!existingMap.TryGetValue(tag.NormalizedName, out var tagEntity))
            {
                tagEntity = new Tag { Name = tag.DisplayName, NormalizedName = tag.NormalizedName };
                db.Tags.Add(tagEntity);
                existingMap[tag.NormalizedName] = tagEntity;
            }

            project.ProjectTags.Add(isNewProject
                ? new ProjectTag { Project = project, Tag = tagEntity }
                : new ProjectTag { ProjectId = project.Id, Tag = tagEntity });
        }
    }

    private static ProjectDto MapProjectDto(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        StartDate = project.StartDate.ToString("yyyy-MM-dd"),
        EndDate = project.EndDate?.ToString("yyyy-MM-dd"),
        Description = project.Description,
        Version = project.Version,
        Tags = project.ProjectTags.Select(pt => pt.Tag?.Name ?? string.Empty).ToList()
    };
}

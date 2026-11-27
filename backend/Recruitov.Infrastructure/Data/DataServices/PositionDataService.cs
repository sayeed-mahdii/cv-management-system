using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Recruitov.Application.Core.Helpers;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;

namespace Recruitov.Infrastructure.Data.DataServices;

public class PositionDataService(AppDbContext db) : IPositionDataService
{
    public async Task<PagedResult<PositionSummaryDto>> GetPositionsAsync(PositionSearchQuery query, CancellationToken ct = default)
    {
        var dbQuery = db.Positions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(p =>
                p.Title.ToLower().Contains(search) ||
                p.ShortDescription.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            var tagNorm = TagNormalizationHelper.NormalizeSingle(query.Tag);
            dbQuery = dbQuery.Where(p => p.PositionTags.Any(pt => pt.Tag!.NormalizedName == tagNorm));
        }

        dbQuery = dbQuery.OrderByDescending(p => p.UpdatedAt);

        var totalCount = await dbQuery.CountAsync(ct);
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize < 1 ? 10 : query.PageSize, 1, 100);

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
            .ToListAsync(ct);

        return new PagedResult<PositionSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PositionDetailDto?> GetPositionByIdAsync(int id, string? currentUserId, CancellationToken ct = default)
    {
        var position = await GetPositionDetailQuery(id).FirstOrDefaultAsync(ct);
        if (position is null) return null;

        if (!string.IsNullOrEmpty(currentUserId))
            position.HasApplied = await db.Cvs.AnyAsync(c => c.PositionId == id && c.UserId == currentUserId, ct);

        return position;
    }

    public async Task<PositionDetailDto> CreatePositionAsync(CreatePositionDto dto, CancellationToken ct = default)
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

        if (dto.AttributeIds is { Count: > 0 })
        {
            var validAttrIds = await db.AttributeDefinitions
                .Where(a => dto.AttributeIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync(ct);

            var validSet = validAttrIds.ToHashSet();
            var ordered = dto.AttributeIds.Where(validSet.Contains).Distinct().ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                position.PositionAttributes.Add(new PositionAttribute
                {
                    AttributeDefinitionId = ordered[i],
                    SortOrder = i + 1
                });
            }
        }

        var normalizedTags = TagNormalizationHelper.NormalizeTags(dto.Tags);
        if (normalizedTags.Count > 0)
        {
            var normNames = normalizedTags.Select(t => t.NormalizedName).ToList();
            var existingTags = await db.Tags.Where(t => normNames.Contains(t.NormalizedName)).ToListAsync(ct);
            var tagMap = existingTags.ToDictionary(t => t.NormalizedName);

            foreach (var tag in normalizedTags)
            {
                if (!tagMap.TryGetValue(tag.NormalizedName, out var tagEntity))
                {
                    tagEntity = new Tag { Name = tag.DisplayName, NormalizedName = tag.NormalizedName };
                    db.Tags.Add(tagEntity);
                    tagMap[tag.NormalizedName] = tagEntity;
                }

                position.PositionTags.Add(new PositionTag { Position = position, Tag = tagEntity });
            }
        }

        db.Positions.Add(position);
        await db.SaveChangesAsync(ct);

        return await GetPositionDetailQuery(position.Id).FirstAsync(ct);
    }

    public async Task<ErrorOr<PositionDetailDto>> UpdatePositionAsync(int id, UpdatePositionDto dto, CancellationToken ct = default)
    {
        var position = await db.Positions
            .Include(p => p.PositionAttributes)
            .Include(p => p.PositionTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (position is null)
            return Error.NotFound("Position.NotFound", $"Position with ID {id} not found.");

        if (position.Version != dto.Version)
        {
            return Error.Conflict(
                "Position.Concurrency",
                "This position was modified by another user. Please reload the latest changes.",
                new Dictionary<string, object> { ["currentVersion"] = position.Version });
        }

        position.Title = dto.Title.Trim();
        position.ShortDescription = dto.ShortDescription.Trim();
        position.MaxProjects = Math.Clamp(dto.MaxProjects, 1, 10);
        position.Version++;
        position.UpdatedAt = DateTime.UtcNow;

        // Reconcile Attributes
        var targetAttrIds = new List<int>();
        if (dto.AttributeIds is { Count: > 0 })
        {
            var validSet = (await db.AttributeDefinitions
                .Where(a => dto.AttributeIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync(ct)).ToHashSet();

            targetAttrIds = dto.AttributeIds.Where(validSet.Contains).Distinct().ToList();
        }

        var targetAttrSet = targetAttrIds.ToHashSet();
        foreach (var attr in position.PositionAttributes.Where(pa => !targetAttrSet.Contains(pa.AttributeDefinitionId)).ToList())
            position.PositionAttributes.Remove(attr);

        for (int i = 0; i < targetAttrIds.Count; i++)
        {
            var attrId = targetAttrIds[i];
            var existing = position.PositionAttributes.FirstOrDefault(pa => pa.AttributeDefinitionId == attrId);
            if (existing is not null)
                existing.SortOrder = i + 1;
            else
                position.PositionAttributes.Add(new PositionAttribute { PositionId = position.Id, AttributeDefinitionId = attrId, SortOrder = i + 1 });
        }

        // Reconcile Tags
        var normalizedTags = TagNormalizationHelper.NormalizeTags(dto.Tags);
        var targetTagNormSet = normalizedTags.Select(t => t.NormalizedName).ToHashSet();

        foreach (var pt in position.PositionTags.Where(pt => pt.Tag == null || !targetTagNormSet.Contains(pt.Tag.NormalizedName)).ToList())
            position.PositionTags.Remove(pt);

        if (normalizedTags.Count > 0)
        {
            var normNames = normalizedTags.Select(t => t.NormalizedName).ToList();
            var existingTags = await db.Tags.Where(t => normNames.Contains(t.NormalizedName)).ToListAsync(ct);
            var tagMap = existingTags.ToDictionary(t => t.NormalizedName);
            var currentTagNorms = position.PositionTags
                .Where(pt => pt.Tag != null)
                .Select(pt => pt.Tag!.NormalizedName)
                .ToHashSet();

            foreach (var tag in normalizedTags)
            {
                if (currentTagNorms.Contains(tag.NormalizedName)) continue;

                if (!tagMap.TryGetValue(tag.NormalizedName, out var tagEntity))
                {
                    tagEntity = new Tag { Name = tag.DisplayName, NormalizedName = tag.NormalizedName };
                    db.Tags.Add(tagEntity);
                    tagMap[tag.NormalizedName] = tagEntity;
                }

                position.PositionTags.Add(new PositionTag { PositionId = position.Id, Tag = tagEntity });
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict("Position.Concurrency", "Concurrency conflict: Position was modified concurrently.");
        }

        return await GetPositionDetailQuery(position.Id).FirstAsync(ct);
    }

    public async Task<ErrorOr<PositionDetailDto>> DuplicatePositionAsync(int id, CancellationToken ct = default)
    {
        var original = await db.Positions
            .Include(p => p.PositionAttributes)
            .Include(p => p.PositionTags)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (original is null)
            return Error.NotFound("Position.NotFound", $"Position with ID {id} not found.");

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
            clone.PositionAttributes.Add(new PositionAttribute { AttributeDefinitionId = attr.AttributeDefinitionId, SortOrder = attr.SortOrder });

        foreach (var pt in original.PositionTags)
            clone.PositionTags.Add(new PositionTag { TagId = pt.TagId });

        db.Positions.Add(clone);
        await db.SaveChangesAsync(ct);

        return await GetPositionDetailQuery(clone.Id).FirstAsync(ct);
    }

    public async Task<ErrorOr<List<ApplicantSummaryDto>>> GetApplicantsAsync(int positionId, CancellationToken ct = default)
    {
        if (!await db.Positions.AnyAsync(p => p.Id == positionId, ct))
            return Error.NotFound("Position.NotFound", $"Position with ID {positionId} not found.");

        var applicants = await db.Cvs
            .AsNoTracking()
            .Where(c => c.PositionId == positionId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.UserId,
                Email = c.User != null ? c.User.Email : string.Empty,
                FirstName = c.User != null ? c.User.AttributeValues.Where(v => v.AttributeDefinitionId == 1).Select(v => v.Value).FirstOrDefault() : null,
                LastName = c.User != null ? c.User.AttributeValues.Where(v => v.AttributeDefinitionId == 2).Select(v => v.Value).FirstOrDefault() : null,
                Location = c.User != null ? c.User.AttributeValues.Where(v => v.AttributeDefinitionId == 3).Select(v => v.Value).FirstOrDefault() : null,
                PhotoUrl = c.User != null ? c.User.AttributeValues.Where(v => v.AttributeDefinitionId == 4).Select(v => v.Value).FirstOrDefault() : null,
                State = c.State.ToString(),
                c.CreatedAt,
                c.Version
            })
            .ToListAsync(ct);

        return applicants.Select(a =>
        {
            var fullName = $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = a.Email;

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
    }

    public async Task<ErrorOr<ApplicantDetailDto>> GetApplicantDetailAsync(int positionId, string candidateId, CancellationToken ct = default)
    {
        var position = await db.Positions
            .AsNoTracking()
            .Include(p => p.PositionTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.PositionAttributes).ThenInclude(pa => pa.AttributeDefinition).ThenInclude(ad => ad!.Category)
            .Include(p => p.PositionAttributes).ThenInclude(pa => pa.AttributeDefinition).ThenInclude(ad => ad!.Options)
            .FirstOrDefaultAsync(p => p.Id == positionId, ct);

        if (position is null)
            return Error.NotFound("Position.NotFound", $"Position with ID {positionId} not found.");

        var cv = await db.Cvs.AsNoTracking().FirstOrDefaultAsync(c => c.PositionId == positionId && c.UserId == candidateId, ct);
        if (cv is null)
            return Error.NotFound("Cv.NotFound", "Candidate application not found for this position.");

        var candidate = await db.Users
            .AsNoTracking()
            .Include(u => u.AttributeValues).ThenInclude(av => av.AttributeDefinition).ThenInclude(ad => ad!.Category)
            .Include(u => u.AttributeValues).ThenInclude(av => av.AttributeDefinition).ThenInclude(ad => ad!.Options)
            .FirstOrDefaultAsync(u => u.Id == candidateId, ct);

        if (candidate is null)
            return Error.NotFound("Candidate.NotFound", $"Candidate with ID {candidateId} not found.");

        var meAttrs = new List<UserAttributeValueDto>();
        var infoAttrs = new List<UserAttributeValueDto>();
        var userAttrMap = new Dictionary<int, UserAttributeValue>();

        foreach (var av in candidate.AttributeValues)
        {
            userAttrMap[av.AttributeDefinitionId] = av;
            var def = av.AttributeDefinition;
            if (def is null) continue;

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

            if (def.IsBuiltIn) meAttrs.Add(dto);
            else infoAttrs.Add(dto);
        }

        var requiredCheck = new List<ApplicantAttributeMatchDto>();
        foreach (var pa in position.PositionAttributes.OrderBy(pa => pa.SortOrder))
        {
            var def = pa.AttributeDefinition;
            if (def is null) continue;

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

        var allProjects = await db.Projects
            .AsNoTracking()
            .Where(p => p.UserId == candidateId)
            .OrderByDescending(p => p.StartDate)
            .Include(p => p.ProjectTags).ThenInclude(pt => pt.Tag)
            .ToListAsync(ct);

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

            selectedProjects = (matching.Count > 0 ? matching : allProjects).Take(position.MaxProjects).ToList();
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
        if (string.IsNullOrWhiteSpace(candidateFullName)) candidateFullName = candidate.Email;

        return new ApplicantDetailDto
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
        };
    }

    private IQueryable<PositionDetailDto> GetPositionDetailQuery(int id) =>
        db.Positions
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
            });
}

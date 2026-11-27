using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;
using Recruitov.Domain.Enums;

namespace Recruitov.Infrastructure.Data.DataServices;

public class AttributeDataService(
    AppDbContext db,
    ILogger<AttributeDataService> logger) : IAttributeDataService
{
    public async Task<PagedResult<AttributeDto>> GetAttributesAsync(AttributeSearchQuery query, CancellationToken ct = default)
    {
        var dbQuery = db.AttributeDefinitions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(a => a.NormalizedName.StartsWith(search));
        }

        if (query.CategoryId.HasValue && query.CategoryId.Value > 0)
            dbQuery = dbQuery.Where(a => a.CategoryId == query.CategoryId.Value);

        if (query.RecentlyUsedOnly == true)
            dbQuery = dbQuery.Where(a => a.LastUsedAt != null);

        dbQuery = query.RecentlyUsedOnly == true
            ? dbQuery.OrderByDescending(a => a.LastUsedAt)
            : dbQuery.OrderBy(a => a.Name);

        var totalCount = await dbQuery.CountAsync(ct);
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize < 1 ? 10 : query.PageSize, 1, 100);

        var items = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AttributeDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                DataType = a.DataType.ToString(),
                CategoryId = a.CategoryId,
                CategoryName = a.Category != null ? a.Category.Name : string.Empty,
                IsBuiltIn = a.IsBuiltIn,
                LastUsedAt = a.LastUsedAt,
                Version = a.Version,
                Options = a.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
            })
            .ToListAsync(ct);

        return new PagedResult<AttributeDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<AttributeDto?> GetAttributeByIdAsync(int id, CancellationToken ct = default) =>
        db.AttributeDefinitions
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AttributeDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                DataType = a.DataType.ToString(),
                CategoryId = a.CategoryId,
                CategoryName = a.Category != null ? a.Category.Name : string.Empty,
                IsBuiltIn = a.IsBuiltIn,
                LastUsedAt = a.LastUsedAt,
                Version = a.Version,
                Options = a.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
            })
            .FirstOrDefaultAsync(ct);

    public async Task<ErrorOr<AttributeDto>> CreateAttributeAsync(
        CreateAttributeDto dto,
        string? actorName,
        CancellationToken ct = default)
    {
        var normalized = dto.Name.Trim().ToLowerInvariant();

        if (await db.AttributeDefinitions.AnyAsync(a => a.NormalizedName == normalized, ct))
            return Error.Validation("Attribute.DuplicateName", $"An attribute with the name '{dto.Name}' already exists.");

        var category = await db.Categories.FindAsync([dto.CategoryId], ct);
        if (category is null)
            return Error.Validation("Attribute.InvalidCategory", "Selected category does not exist.");

        var attribute = new AttributeDefinition
        {
            Name = dto.Name.Trim(),
            NormalizedName = normalized,
            Description = dto.Description?.Trim(),
            DataType = dto.DataType,
            CategoryId = dto.CategoryId,
            IsBuiltIn = false,
            Version = 1,
            LastUsedAt = null
        };

        if (dto.DataType == DataType.OneOfMany && dto.Options is not null)
        {
            var validOptions = dto.Options.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
            for (int i = 0; i < validOptions.Count; i++)
            {
                attribute.Options.Add(new AttributeOption
                {
                    Label = validOptions[i].Trim(),
                    SortOrder = i + 1
                });
            }
        }

        db.AttributeDefinitions.Add(attribute);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created new attribute: {Name} (ID: {Id}) by user {User}", attribute.Name, attribute.Id, actorName);

        return new AttributeDto
        {
            Id = attribute.Id,
            Name = attribute.Name,
            Description = attribute.Description,
            DataType = attribute.DataType.ToString(),
            CategoryId = attribute.CategoryId,
            CategoryName = category.Name,
            IsBuiltIn = attribute.IsBuiltIn,
            LastUsedAt = attribute.LastUsedAt,
            Version = attribute.Version,
            Options = attribute.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
        };
    }

    public async Task<ErrorOr<AttributeDto>> UpdateAttributeAsync(
        int id,
        UpdateAttributeDto dto,
        CancellationToken ct = default)
    {
        var attribute = await db.AttributeDefinitions
            .Include(a => a.Options)
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (attribute is null)
            return Error.NotFound("Attribute.NotFound", $"Attribute with ID {id} not found.");

        if (attribute.IsBuiltIn)
            return Error.Validation("Attribute.BuiltIn", "Built-in profile attributes (e.g. First Name, Photo) cannot be modified.");

        if (attribute.Version != dto.Version)
        {
            return Error.Conflict(
                "Attribute.Concurrency",
                "This attribute was modified by another user while you were viewing it. Please reload the latest changes.",
                new Dictionary<string, object> { ["currentVersion"] = attribute.Version });
        }

        var normalized = dto.Name.Trim().ToLowerInvariant();
        if (normalized != attribute.NormalizedName &&
            await db.AttributeDefinitions.AnyAsync(a => a.NormalizedName == normalized && a.Id != id, ct))
        {
            return Error.Validation("Attribute.DuplicateName", $"An attribute with the name '{dto.Name}' already exists.");
        }

        var category = await db.Categories.FindAsync([dto.CategoryId], ct);
        if (category is null)
            return Error.Validation("Attribute.InvalidCategory", "Selected category does not exist.");

        attribute.Name = dto.Name.Trim();
        attribute.NormalizedName = normalized;
        attribute.Description = dto.Description?.Trim();
        attribute.CategoryId = dto.CategoryId;
        attribute.Version++;

        if (attribute.DataType == DataType.OneOfMany && dto.Options is not null)
        {
            db.AttributeOptions.RemoveRange(attribute.Options);
            var validOptions = dto.Options.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
            for (int i = 0; i < validOptions.Count; i++)
            {
                attribute.Options.Add(new AttributeOption
                {
                    AttributeDefinitionId = attribute.Id,
                    Label = validOptions[i].Trim(),
                    SortOrder = i + 1
                });
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict("Attribute.Concurrency", "This attribute was modified by another user. Please reload.");
        }

        return new AttributeDto
        {
            Id = attribute.Id,
            Name = attribute.Name,
            Description = attribute.Description,
            DataType = attribute.DataType.ToString(),
            CategoryId = attribute.CategoryId,
            CategoryName = category.Name,
            IsBuiltIn = attribute.IsBuiltIn,
            LastUsedAt = attribute.LastUsedAt,
            Version = attribute.Version,
            Options = attribute.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
        };
    }

    public async Task<ErrorOr<Deleted>> DeleteAttributeAsync(int id, CancellationToken ct = default)
    {
        var attribute = await db.AttributeDefinitions.FindAsync([id], ct);
        if (attribute is null)
            return Error.NotFound("Attribute.NotFound", $"Attribute with ID {id} not found.");

        if (attribute.IsBuiltIn)
            return Error.Validation("Attribute.BuiltIn", "Built-in profile attributes cannot be deleted.");

        db.AttributeDefinitions.Remove(attribute);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Deleted attribute {Name} (ID: {Id})", attribute.Name, id);

        return Result.Deleted;
    }
}

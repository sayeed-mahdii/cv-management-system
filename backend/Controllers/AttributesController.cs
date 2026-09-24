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
[Authorize]
public class AttributesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AttributesController> _logger;

    public AttributesController(AppDbContext db, ILogger<AttributesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Search and list attribute definitions with server-side paging and category filtering.
    /// Efficient database projection without SELECT * scans.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AttributeDto>>> GetAttributes([FromQuery] AttributeSearchQuery query)
    {
        var dbQuery = _db.AttributeDefinitions.AsNoTracking().AsQueryable();

        // 1. Prefix search by name (e.g. typing "CA" finds "CAP")
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(a => a.NormalizedName.StartsWith(search));
        }

        // 2. Filter by Category
        if (query.CategoryId.HasValue && query.CategoryId.Value > 0)
        {
            dbQuery = dbQuery.Where(a => a.CategoryId == query.CategoryId.Value);
        }

        // 3. Filter by recently used
        if (query.RecentlyUsedOnly == true)
        {
            dbQuery = dbQuery.Where(a => a.LastUsedAt != null);
        }

        // 4. Order results (recently used first, then alphabetical)
        dbQuery = query.RecentlyUsedOnly == true
            ? dbQuery.OrderByDescending(a => a.LastUsedAt)
            : dbQuery.OrderBy(a => a.Name);

        var totalCount = await dbQuery.CountAsync();
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : (query.PageSize > 100 ? 100 : query.PageSize);

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
            .ToListAsync();

        return Ok(new PagedResult<AttributeDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Gets a single attribute definition by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AttributeDto>> GetAttribute(int id)
    {
        var attribute = await _db.AttributeDefinitions
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
            .FirstOrDefaultAsync();

        if (attribute == null)
        {
            return NotFound(new { message = $"Attribute with ID {id} not found." });
        }

        return Ok(attribute);
    }

    /// <summary>
    /// Creates a new attribute definition in the Attribute Library (Spec §4.3).
    /// Enforces globally unique name. Restricted to Recruiters and Administrators.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<AttributeDto>> CreateAttribute([FromBody] CreateAttributeDto dto)
    {
        var normalized = dto.Name.Trim().ToLowerInvariant();

        // Enforce globally unique name
        var exists = await _db.AttributeDefinitions.AnyAsync(a => a.NormalizedName == normalized);
        if (exists)
        {
            return BadRequest(new { message = $"An attribute with the name '{dto.Name}' already exists." });
        }

        // Validate category
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
        {
            return BadRequest(new { message = "Selected category does not exist." });
        }

        // Validate OneOfMany options
        if (dto.DataType == DataType.OneOfMany)
        {
            if (dto.Options == null || dto.Options.Count == 0 || dto.Options.All(string.IsNullOrWhiteSpace))
            {
                return BadRequest(new { message = "At least one option is required for a 'One of many' attribute." });
            }
        }

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

        if (dto.DataType == DataType.OneOfMany && dto.Options != null)
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

        _db.AttributeDefinitions.Add(attribute);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created new attribute: {Name} (ID: {Id}) by user {User}",
            attribute.Name, attribute.Id, User.Identity?.Name);

        // Fetch category name for response
        var category = await _db.Categories.FindAsync(attribute.CategoryId);

        return CreatedAtAction(nameof(GetAttribute), new { id = attribute.Id }, new AttributeDto
        {
            Id = attribute.Id,
            Name = attribute.Name,
            Description = attribute.Description,
            DataType = attribute.DataType.ToString(),
            CategoryId = attribute.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            IsBuiltIn = attribute.IsBuiltIn,
            LastUsedAt = attribute.LastUsedAt,
            Version = attribute.Version,
            Options = attribute.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).ToList()
        });
    }

    /// <summary>
    /// Updates an existing attribute definition with Optimistic Concurrency check.
    /// Built-in attributes cannot be modified.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<ActionResult<AttributeDto>> UpdateAttribute(int id, [FromBody] UpdateAttributeDto dto)
    {
        var attribute = await _db.AttributeDefinitions
            .Include(a => a.Options)
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attribute == null)
        {
            return NotFound(new { message = $"Attribute with ID {id} not found." });
        }

        // Built-in attributes cannot be modified by recruiters
        if (attribute.IsBuiltIn)
        {
            return BadRequest(new { message = "Built-in profile attributes (e.g. First Name, Photo) cannot be modified." });
        }

        // Optimistic concurrency check (Spec §9)
        if (attribute.Version != dto.Version)
        {
            return Conflict(new
            {
                message = "This attribute was modified by another user while you were viewing it. Please reload the latest changes.",
                currentVersion = attribute.Version
            });
        }

        var normalized = dto.Name.Trim().ToLowerInvariant();

        // Enforce unique name if changed
        if (normalized != attribute.NormalizedName)
        {
            var exists = await _db.AttributeDefinitions.AnyAsync(a => a.NormalizedName == normalized && a.Id != id);
            if (exists)
            {
                return BadRequest(new { message = $"An attribute with the name '{dto.Name}' already exists." });
            }
        }

        // Validate category
        var category = await _db.Categories.FindAsync(dto.CategoryId);
        if (category == null)
        {
            return BadRequest(new { message = "Selected category does not exist." });
        }

        attribute.Name = dto.Name.Trim();
        attribute.NormalizedName = normalized;
        attribute.Description = dto.Description?.Trim();
        attribute.CategoryId = dto.CategoryId;
        attribute.Version++; // Increment version on update

        // Update options if OneOfMany
        if (attribute.DataType == DataType.OneOfMany && dto.Options != null)
        {
            _db.AttributeOptions.RemoveRange(attribute.Options);
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
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This attribute was modified by another user. Please reload." });
        }

        return Ok(new AttributeDto
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
        });
    }

    /// <summary>
    /// Deletes an attribute definition. Built-in attributes cannot be deleted.
    /// Cascades cleanly to position links and user attribute values.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<IActionResult> DeleteAttribute(int id)
    {
        var attribute = await _db.AttributeDefinitions.FindAsync(id);

        if (attribute == null)
        {
            return NotFound(new { message = $"Attribute with ID {id} not found." });
        }

        // Built-in attributes cannot be deleted
        if (attribute.IsBuiltIn)
        {
            return BadRequest(new { message = "Built-in profile attributes cannot be deleted." });
        }

        _db.AttributeDefinitions.Remove(attribute);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted attribute {Name} (ID: {Id})", attribute.Name, id);
        return NoContent();
    }
}

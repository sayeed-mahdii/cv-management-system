using System.ComponentModel.DataAnnotations;
using DataType = backend.Models.Enums.DataType;

namespace backend.Models.DTOs;

public class AttributeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int Version { get; set; }
    public List<string> Options { get; set; } = new();
}

public class CreateAttributeDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DataType DataType { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public List<string>? Options { get; set; }
}

public class UpdateAttributeDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int Version { get; set; }

    public List<string>? Options { get; set; }
}

public class AttributeSearchQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool? RecentlyUsedOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

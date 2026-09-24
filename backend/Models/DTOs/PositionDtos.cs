using System.ComponentModel.DataAnnotations;

namespace backend.Models.DTOs;

public class PositionSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int MaxProjects { get; set; }
    public int Version { get; set; }
    public int ApplicationsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> AttributeNames { get; set; } = new();
}

public class PositionDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int MaxProjects { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PositionRequiredAttributeDto> Attributes { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public int ApplicationsCount { get; set; }
    public bool HasApplied { get; set; }
}

public class PositionRequiredAttributeDto
{
    public int AttributeDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<string> Options { get; set; } = new();
}

public class CreatePositionDto
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string ShortDescription { get; set; } = string.Empty;

    [Range(1, 10)]
    public int MaxProjects { get; set; } = 3;

    public List<int> AttributeIds { get; set; } = new();

    public List<string> Tags { get; set; } = new();
}

public class UpdatePositionDto
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string ShortDescription { get; set; } = string.Empty;

    [Range(1, 10)]
    public int MaxProjects { get; set; } = 3;

    [Required]
    public int Version { get; set; }

    public List<int> AttributeIds { get; set; } = new();

    public List<string> Tags { get; set; } = new();
}

public class PositionSearchQuery
{
    public string? Search { get; set; }
    public string? Tag { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

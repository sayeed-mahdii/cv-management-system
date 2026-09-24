using System.ComponentModel.DataAnnotations;

namespace backend.Models.DTOs;

public class ProfileResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredTheme { get; set; }
    public bool IsBlocked { get; set; }
    public List<string> Roles { get; set; } = new();

    public List<UserAttributeValueDto> MeAttributes { get; set; } = new();
    public List<UserAttributeValueDto> InfoAttributes { get; set; } = new();
    public List<ProjectDto> Projects { get; set; } = new();
    public List<ApplicationSummaryDto> Applications { get; set; } = new();
}

public class UserAttributeValueDto
{
    public int AttributeDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? ImagePublicId { get; set; }
    public bool IsBuiltIn { get; set; }
    public List<string> Options { get; set; } = new();
}

public class UpdateProfileDto
{
    [Required]
    public int Version { get; set; }

    public List<AttributeValueInputDto> Attributes { get; set; } = new();
}

public class AttributeValueInputDto
{
    [Required]
    public int AttributeDefinitionId { get; set; }

    public string Value { get; set; } = string.Empty;

    public string? ImagePublicId { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class CreateProjectDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Description { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();
}

public class UpdateProjectDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Description { get; set; } = string.Empty;

    [Required]
    public int Version { get; set; }

    public List<string> Tags { get; set; } = new();
}

public class ApplicationSummaryDto
{
    public int CvId { get; set; }
    public int PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CloudinarySignatureDto
{
    public bool IsConfigured { get; set; }
    public string Signature { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string CloudName { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
}

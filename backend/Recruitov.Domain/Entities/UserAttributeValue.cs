namespace Recruitov.Domain.Entities;

/// <summary>
/// EAV storage: maps a candidate user to an attribute definition with their specific value.
/// Stores both built-in "Me" values and custom library values.
/// Composite PK: (UserId, AttributeDefinitionId).
/// </summary>
public class UserAttributeValue
{
    public string UserId { get; set; } = string.Empty;

    public int AttributeDefinitionId { get; set; }

    /// <summary>
    /// Canonical string format:
    /// - Numeric: invariant decimal (e.g. 7.5)
    /// - Date: ISO yyyy-MM-dd
    /// - Period: start/end ISO string
    /// - OneOfMany: AttributeOption.Id as string
    /// - Image: Cloudinary secure URL
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Cloudinary Public ID for Image attributes to enable clean cloud file deletion.
    /// </summary>
    public string? ImagePublicId { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser? User { get; set; }
    public AttributeDefinition? AttributeDefinition { get; set; }
}

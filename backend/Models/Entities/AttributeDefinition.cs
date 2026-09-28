using backend.Models.Enums;

namespace backend.Models.Entities;

/// <summary>
/// Reusable Attribute Library Definition.
/// Defines globally available attributes. Names are globally unique.
/// Built-in attributes (First Name, Photo, etc.) have IsBuiltIn = true and cannot be deleted.
/// </summary>
public class AttributeDefinition
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Lowercase and trimmed for case-insensitive unique constraint and text_pattern_ops prefix lookup.
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DataType DataType { get; set; }

    public int CategoryId { get; set; }

    /// <summary>
    /// True for mandatory system attributes (First Name, Last Name, Location, Personal Photo).
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;

    /// <summary>
    /// Updated whenever selected in a profile or position to support "Recently Used" queries.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Concurrency token for Optimistic Locking.
    /// </summary>
    public int Version { get; set; } = 1;

    // Navigation properties
    public Category? Category { get; set; }
    public ICollection<AttributeOption> Options { get; set; } = new List<AttributeOption>();
    public ICollection<UserAttributeValue> Values { get; set; } = new List<UserAttributeValue>();
    public ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public ICollection<PositionAccessRule> PositionAccessRules { get; set; } = new List<PositionAccessRule>();
}

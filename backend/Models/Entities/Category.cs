namespace backend.Models.Entities;

/// <summary>
/// Categories for grouping attributes.
/// Predefined list (Certification, Domain Knowledge, Soft Skills).
/// App owners modify via DB; no UI required.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Navigation property
    public ICollection<AttributeDefinition> Attributes { get; set; } = new List<AttributeDefinition>();
}

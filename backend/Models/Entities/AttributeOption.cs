namespace backend.Models.Entities;

/// <summary>
/// Predefined choices for attributes with DataType = OneOfMany (Spec §4.3).
/// Example: "Essentials", "Pro", "Expert".
/// </summary>
public class AttributeOption
{
    public int Id { get; set; }

    public int AttributeDefinitionId { get; set; }

    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;

    // Navigation property
    public AttributeDefinition? AttributeDefinition { get; set; }
}

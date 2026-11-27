namespace Recruitov.Domain.Entities;

/// <summary>
/// Links an Attribute Definition to a Position template.
/// Composite PK: (PositionId, AttributeDefinitionId).
/// </summary>
public class PositionAttribute
{
    public int PositionId { get; set; }
    public int AttributeDefinitionId { get; set; }

    /// <summary>
    /// Display sort order of attributes in the generated CV.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // Navigation properties
    public Position? Position { get; set; }
    public AttributeDefinition? AttributeDefinition { get; set; }
}

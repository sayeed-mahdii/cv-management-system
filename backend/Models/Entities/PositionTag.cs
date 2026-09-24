namespace backend.Models.Entities;

/// <summary>
/// Technology tags required by a Position to filter relevant candidate projects.
/// Composite PK: (PositionId, TagId).
/// </summary>
public class PositionTag
{
    public int PositionId { get; set; }
    public int TagId { get; set; }

    // Navigation properties
    public Position? Position { get; set; }
    public Tag? Tag { get; set; }
}

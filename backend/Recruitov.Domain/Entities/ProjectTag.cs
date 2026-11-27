namespace Recruitov.Domain.Entities;

/// <summary>
/// Many-to-many junction between Project and Tag.
/// Composite PK: (ProjectId, TagId).
/// </summary>
public class ProjectTag
{
    public int ProjectId { get; set; }
    public int TagId { get; set; }

    // Navigation properties
    public Project? Project { get; set; }
    public Tag? Tag { get; set; }
}

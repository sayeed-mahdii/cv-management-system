namespace backend.Models.Entities;

/// <summary>
/// A candidate's portfolio project (Spec §4.2).
/// Includes Name, Date range, Markdown Description, and Tags.
/// </summary>
public class Project
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Concurrency token for Optimistic Locking.
    /// </summary>
    public int Version { get; set; } = 1;

    // Navigation properties
    public AppUser? User { get; set; }
    public ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
}

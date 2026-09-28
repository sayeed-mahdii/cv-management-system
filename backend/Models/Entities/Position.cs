namespace backend.Models.Entities;

/// <summary>
/// Customizable Position CV Template.
/// Managed by Recruiters. Defines required attributes, project filter tags, and access rules.
/// </summary>
public class Position
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// If true, all authenticated candidates can view and create CVs.
    /// If false, candidate must satisfy all PositionAccessRules.
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Maximum recent candidate projects to include in the dynamically generated CV.
    /// </summary>
    public int MaxProjects { get; set; } = 3;

    /// <summary>
    /// Concurrency token for Optimistic Locking.
    /// </summary>
    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public ICollection<PositionTag> PositionTags { get; set; } = new List<PositionTag>();
    public ICollection<PositionAccessRule> PositionAccessRules { get; set; } = new List<PositionAccessRule>();
    public ICollection<Cv> Cvs { get; set; } = new List<Cv>();
    public ICollection<DiscussionPost> DiscussionPosts { get; set; } = new List<DiscussionPost>();
}

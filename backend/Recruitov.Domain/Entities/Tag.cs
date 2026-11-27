namespace Recruitov.Domain.Entities;

/// <summary>
/// Technology and skill tags.
/// Created in the database on first use.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Lowercase normalized name to prevent case-sensitivity duplicates ("React" vs "react").
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
    public ICollection<PositionTag> PositionTags { get; set; } = new List<PositionTag>();
}

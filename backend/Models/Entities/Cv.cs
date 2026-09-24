using backend.Models.Enums;

namespace backend.Models.Entities;

/// <summary>
/// Automatically Generated Virtual CV (Killer Feature #3, Spec §4.5).
/// Holds metadata, state, and version. Content is dynamically assembled on read.
/// Strictly at most one CV per candidate per position.
/// </summary>
public class Cv
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int PositionId { get; set; }

    public CvState State { get; set; } = CvState.Draft;

    /// <summary>
    /// Concurrency token for Optimistic Locking.
    /// </summary>
    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser? User { get; set; }
    public Position? Position { get; set; }
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}

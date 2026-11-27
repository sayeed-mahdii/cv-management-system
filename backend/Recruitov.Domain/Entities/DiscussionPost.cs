namespace Recruitov.Domain.Entities;

/// <summary>
/// Chronological discussion posts attached to a Position.
/// Author name links to profile. Supports Markdown.
/// Updates polled every 2-5 seconds.
/// </summary>
public class DiscussionPost
{
    public int Id { get; set; }

    public int PositionId { get; set; }

    public string AuthorUserId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Position? Position { get; set; }
    public AppUser? AuthorUser { get; set; }
}

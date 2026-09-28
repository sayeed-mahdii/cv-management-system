namespace backend.Models.Entities;

/// <summary>
/// CV likes given by recruiters.
/// Composite PK: (CvId, RecruiterUserId) enforces at most one like per recruiter per CV.
/// </summary>
public class Like
{
    public int CvId { get; set; }

    public string RecruiterUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Cv? Cv { get; set; }
    public AppUser? RecruiterUser { get; set; }
}

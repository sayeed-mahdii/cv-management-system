namespace backend.Models.Entities;

/// <summary>
/// Represents an authenticated user in the system.
/// The Id matches the unique UUID issued by Supabase Auth (sub claim).
/// All personal profile data (First Name, Photo, etc.) is stored in the EAV UserAttributeValues table.
/// </summary>
public class AppUser
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsBlocked { get; set; } = false;

    public string? PreferredLanguage { get; set; } = "en";

    public string? PreferredTheme { get; set; } = "light";

    /// <summary>
    /// Concurrency token for Optimistic Locking.
    /// Bumping this prevents concurrent profile modifications.
    /// </summary>
    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<AppUserRole> Roles { get; set; } = new List<AppUserRole>();
    public ICollection<UserAttributeValue> AttributeValues { get; set; } = new List<UserAttributeValue>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Cv> Cvs { get; set; } = new List<Cv>();
    public ICollection<DiscussionPost> DiscussionPosts { get; set; } = new List<DiscussionPost>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}

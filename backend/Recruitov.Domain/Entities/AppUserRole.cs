namespace Recruitov.Domain.Entities;

/// <summary>
/// Roles assigned to a user (Candidate, Recruiter, Administrator).
/// A user may have multiple roles simultaneously.
/// </summary>
public class AppUserRole
{
    public const string Candidate = "Candidate";
    public const string Recruiter = "Recruiter";
    public const string Administrator = "Administrator";

    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = Candidate;

    // Navigation property
    public AppUser? User { get; set; }
}

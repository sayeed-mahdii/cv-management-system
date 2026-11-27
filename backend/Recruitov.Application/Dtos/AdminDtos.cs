namespace Recruitov.Application.Dtos;

/// <summary>
/// Summary DTO for displaying users in the Admin roster table.
/// </summary>
public class AdminUserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? CandidateName { get; set; }
    public int ProjectsCount { get; set; }
    public int ApplicationsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; }
}

/// <summary>
/// Request payload for bulk or single user block/unblock actions.
/// </summary>
public class BatchBlockUsersRequest
{
    public List<string> UserIds { get; set; } = new();
    public bool IsBlocked { get; set; }
}

/// <summary>
/// Request payload for modifying user roles (assign/remove).
/// </summary>
public class ManageUserRolesRequest
{
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Request payload for bulk deleting users and their associated records.
/// </summary>
public class BatchDeleteUsersRequest
{
    public List<string> UserIds { get; set; } = new();
}

/// <summary>
/// Response payload after updating user roles.
/// </summary>
public class ManageRolesResponse
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool CallerLostAdmin { get; set; }
}

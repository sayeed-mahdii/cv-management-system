namespace Recruitov.Application.Dtos;

public class BuiltInAttributeItemDto
{
    public int AttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? ImagePublicId { get; set; }
}

public class CurrentUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredTheme { get; set; }
    public int Version { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<BuiltInAttributeItemDto> MeAttributes { get; set; } = [];
}

public class UpdatePreferencesDto
{
    public string? PreferredLanguage { get; set; }
    public string? PreferredTheme { get; set; }
}

public class HealthStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SupportTicketContextDto
{
    public string ReportedBy { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string SuggestedPosition { get; set; } = "N/A";
    public List<string> AdminEmails { get; set; } = [];
    public bool HasConfiguredDropboxToken { get; set; }
    public bool HasConfiguredOneDriveToken { get; set; }
}

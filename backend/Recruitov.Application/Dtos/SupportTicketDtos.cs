namespace Recruitov.Application.Dtos;

/// <summary>
/// Payload submitted from the client to generate a support ticket.
/// </summary>
public class CreateSupportTicketDto
{
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Must be one of: High, Moderate, Low.
    /// </summary>
    public string Priority { get; set; } = "Moderate";

    public string? PositionTitle { get; set; }

    public string? Link { get; set; }

    /// <summary>
    /// Target cloud service ("dropbox" or "onedrive"). Defaults to "dropbox".
    /// </summary>
    public string? CloudProvider { get; set; } = "dropbox";

    /// <summary>
    /// Optional access token if supplied via frontend settings/modal.
    /// </summary>
    public string? CloudAccessToken { get; set; }
}

/// <summary>
/// Response returned upon generating and uploading a support ticket.
/// </summary>
public class SupportTicketResponseDto
{
    public bool Success { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FileContentJson { get; set; } = string.Empty;

    public bool UploadedToCloud { get; set; }

    public string CloudProvider { get; set; } = string.Empty;

    public string? CloudPath { get; set; }

    public string Message { get; set; } = string.Empty;

    public Dictionary<string, object> Payload { get; set; } = new();
}

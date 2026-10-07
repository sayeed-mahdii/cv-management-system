using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models.DTOs;
using backend.Models.Entities;
using backend.Services;

namespace backend.Controllers;

/// <summary>
/// Handles generating support tickets, assembling required structured JSON payloads,
/// and uploading them to Dropbox / OneDrive to trigger Power Automate Cloud Flows.
/// </summary>
[ApiController]
[Route("api/support-ticket")]
[AllowAnonymous]
public class SupportTicketController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CloudStorageService _cloudStorageService;
    private readonly IConfiguration _config;
    private readonly ILogger<SupportTicketController> _logger;

    private static readonly HashSet<string> ValidPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "High",
        "Moderate",
        "Average",
        "Low"
    };

    public SupportTicketController(
        AppDbContext db,
        CloudStorageService cloudStorageService,
        IConfiguration config,
        ILogger<SupportTicketController> logger)
    {
        _db = db;
        _cloudStorageService = cloudStorageService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Returns dynamic context (reported by user, position suggestion, admin emails, provider configuration)
    /// to populate and preview the support ticket modal.
    /// </summary>
    [HttpGet("context")]
    public async Task<IActionResult> GetTicketContext([FromQuery] string? positionId = null)
    {
        var (reporterName, userRole) = await ResolveUserInfoAsync();

        var adminEmails = await _db.Users
            .AsNoTracking()
            .Where(u => !u.IsBlocked && u.Roles.Any(r => r.Role == AppUserRole.Administrator))
            .Select(u => u.Email)
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct()
            .ToListAsync();

        if (adminEmails.Count == 0)
        {
            adminEmails.Add("admin@recruitov.com");
        }

        string? suggestedPositionTitle = null;
        if (!string.IsNullOrWhiteSpace(positionId) && int.TryParse(positionId, out var pId))
        {
            suggestedPositionTitle = await _db.Positions
                .AsNoTracking()
                .Where(p => p.Id == pId)
                .Select(p => p.Title)
                .FirstOrDefaultAsync();
        }

        var hasDropboxToken = !string.IsNullOrWhiteSpace(_config["Dropbox:AccessToken"]) ||
                              !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DROPBOX_ACCESS_TOKEN"));
        var hasOneDriveToken = !string.IsNullOrWhiteSpace(_config["OneDrive:AccessToken"]) ||
                               !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ONEDRIVE_ACCESS_TOKEN"));

        return Ok(new
        {
            ReportedBy = reporterName,
            Role = userRole,
            SuggestedPosition = suggestedPositionTitle ?? "N/A",
            AdminEmails = adminEmails,
            HasConfiguredDropboxToken = hasDropboxToken,
            HasConfiguredOneDriveToken = hasOneDriveToken
        });
    }

    /// <summary>
    /// Creates a support ticket, builds the exact JSON schema required by Power Automate,
    /// and uploads it to the configured cloud storage (Dropbox / OneDrive).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SupportTicketResponseDto>> CreateTicket([FromBody] CreateSupportTicketDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Invalid support ticket payload." });
        }

        if (string.IsNullOrWhiteSpace(dto.Summary))
        {
            return BadRequest(new { message = "Summary field is required." });
        }

        if (string.IsNullOrWhiteSpace(dto.Priority) || !ValidPriorities.Contains(dto.Priority.Trim()))
        {
            return BadRequest(new { message = "Priority must be one of: High, Moderate, Low." });
        }

        // Normalize priority casing to match specification ("High", "Moderate", "Low")
        var normalizedPriority = dto.Priority.Trim().ToLowerInvariant() switch
        {
            "high" => "High",
            "low" => "Low",
            _ => "Moderate"
        };

        // 1. Resolve Reporter information: name and role
        var (reporterName, userRole) = await ResolveUserInfoAsync();

        // 2. Resolve Position/Role: use specific position title if provided, otherwise user role
        var positionOrRole = (!string.IsNullOrWhiteSpace(dto.PositionTitle) && dto.PositionTitle != "N/A")
            ? dto.PositionTitle.Trim()
            : userRole;

        // 3. Resolve Link (fallback to Request Referer header)
        var link = !string.IsNullOrWhiteSpace(dto.Link)
            ? dto.Link.Trim()
            : (Request.Headers.Referer.ToString() ?? "N/A");

        // 4. Resolve Platform Administrator emails
        var adminEmails = await _db.Users
            .AsNoTracking()
            .Where(u => !u.IsBlocked && u.Roles.Any(r => r.Role == AppUserRole.Administrator))
            .Select(u => u.Email)
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct()
            .ToListAsync();

        if (adminEmails.Count == 0)
        {
            adminEmails.Add("admin@recruitov.com");
        }

        // 5. Build the exact JSON dictionary as required by the specification
        var payload = new Dictionary<string, object>
        {
            ["Reported by"] = reporterName,
            ["Position/Role"] = positionOrRole,
            ["Link"] = link,
            ["Priority"] = normalizedPriority,
            ["Admins' e-mail addresses"] = adminEmails,
            ["Summary"] = dto.Summary.Trim()
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var fileContentJson = JsonSerializer.Serialize(payload, jsonOptions);

        // 6. Generate distinct timestamped filename
        var fileName = $"ticket_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}.json";
        var provider = string.IsNullOrWhiteSpace(dto.CloudProvider) ? "dropbox" : dto.CloudProvider.Trim().ToLowerInvariant();

        // 7. Upload to Cloud Storage
        var (uploaded, cloudPath, message) = await _cloudStorageService.UploadJsonFileAsync(
            fileName,
            fileContentJson,
            provider,
            dto.CloudAccessToken);

        _logger.LogInformation(
            "Created support ticket {FileName} for {Reporter} ({PositionOrRole}, Priority: {Priority}). Cloud upload: {Uploaded}",
            fileName, reporterName, positionOrRole, normalizedPriority, uploaded);

        if (!uploaded)
        {
            return BadRequest(new
            {
                message = message,
                fileName = fileName,
                success = false
            });
        }

        return Ok(new SupportTicketResponseDto
        {
            Success = true,
            FileName = fileName,
            FileContentJson = fileContentJson,
            UploadedToCloud = true,
            CloudProvider = provider,
            CloudPath = cloudPath,
            Message = message,
            Payload = payload
        });
    }

    private async Task<(string reporterName, string role)> ResolveUserInfoAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return ("Guest", "Visitor");
        }

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.AttributeValues)
                .ThenInclude(v => v.AttributeDefinition)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return ("Unknown User", "User");
        }

        var firstName = user.AttributeValues
            .Where(v => v.AttributeDefinitionId == 1 || (v.AttributeDefinition != null && v.AttributeDefinition.Name.ToLower() == "first name"))
            .Select(v => v.Value)
            .FirstOrDefault();

        var lastName = user.AttributeValues
            .Where(v => v.AttributeDefinitionId == 2 || (v.AttributeDefinition != null && v.AttributeDefinition.Name.ToLower() == "last name"))
            .Select(v => v.Value)
            .FirstOrDefault();

        var fullName = $"{firstName ?? ""} {lastName ?? ""}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = user.Email;
        }

        // Determine primary role
        var roles = user.Roles.Select(r => r.Role).ToList();
        var primaryRole = roles.Contains(AppUserRole.Administrator)
            ? AppUserRole.Administrator
            : roles.Contains(AppUserRole.Recruiter)
                ? AppUserRole.Recruiter
                : AppUserRole.Candidate;

        return (fullName, primaryRole);
    }
}

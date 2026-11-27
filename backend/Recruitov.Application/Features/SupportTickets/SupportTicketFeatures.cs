using System.Text.Encodings.Web;
using System.Text.Json;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;

namespace Recruitov.Application.Features.SupportTickets;

// --- Get Context ---
public record GetSupportTicketContextQuery(string? UserId, string? PositionId)
    : IRequest<ErrorOr<SupportTicketContextDto>>;

public class GetSupportTicketContextHandler(
    ISupportTicketDataService ticketData,
    ICloudStorageService cloudStorage)
    : IRequestHandler<GetSupportTicketContextQuery, ErrorOr<SupportTicketContextDto>>
{
    public async Task<ErrorOr<SupportTicketContextDto>> Handle(GetSupportTicketContextQuery request, CancellationToken ct)
    {
        var (reporterName, userRole) = await ticketData.ResolveUserInfoAsync(request.UserId, ct);
        var adminEmails = await ticketData.GetActiveAdminEmailsAsync(ct);

        string? suggestedTitle = null;
        if (!string.IsNullOrWhiteSpace(request.PositionId) && int.TryParse(request.PositionId, out var pId))
        {
            suggestedTitle = await ticketData.GetPositionTitleAsync(pId, ct);
        }

        return new SupportTicketContextDto
        {
            ReportedBy = reporterName,
            Role = userRole,
            SuggestedPosition = suggestedTitle ?? "N/A",
            AdminEmails = adminEmails,
            HasConfiguredDropboxToken = cloudStorage.HasConfiguredDropboxToken,
            HasConfiguredOneDriveToken = cloudStorage.HasConfiguredOneDriveToken
        };
    }
}

// --- Create Ticket ---
public record CreateSupportTicketCommand(
    string? UserId,
    string? RefererHeader,
    CreateSupportTicketDto Dto) : IRequest<ErrorOr<SupportTicketResponseDto>>;

public class CreateSupportTicketValidator : AbstractValidator<CreateSupportTicketCommand>
{
    private static readonly HashSet<string> ValidPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "High", "Moderate", "Average", "Low"
    };

    public CreateSupportTicketValidator()
    {
        RuleFor(x => x.Dto)
            .NotNull().WithMessage("Invalid support ticket payload.");

        When(x => x.Dto is not null, () =>
        {
            RuleFor(x => x.Dto.Summary)
                .NotEmpty().WithMessage("Summary field is required.");

            RuleFor(x => x.Dto.Priority)
                .Must(p => !string.IsNullOrWhiteSpace(p) && ValidPriorities.Contains(p.Trim()))
                .WithMessage("Priority must be one of: High, Moderate, Low.");
        });
    }
}

public class CreateSupportTicketHandler(
    ISupportTicketDataService ticketData,
    ICloudStorageService cloudStorage,
    ILogger<CreateSupportTicketHandler> logger)
    : IRequestHandler<CreateSupportTicketCommand, ErrorOr<SupportTicketResponseDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<ErrorOr<SupportTicketResponseDto>> Handle(CreateSupportTicketCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var normalizedPriority = dto.Priority.Trim().ToLowerInvariant() switch
        {
            "high" => "High",
            "low" => "Low",
            _ => "Moderate"
        };

        var (reporterName, userRole) = await ticketData.ResolveUserInfoAsync(request.UserId, ct);
        var positionOrRole = !string.IsNullOrWhiteSpace(dto.PositionTitle) && dto.PositionTitle != "N/A"
            ? dto.PositionTitle.Trim()
            : userRole;

        var link = !string.IsNullOrWhiteSpace(dto.Link)
            ? dto.Link.Trim()
            : (!string.IsNullOrEmpty(request.RefererHeader) ? request.RefererHeader : "N/A");

        var adminEmails = await ticketData.GetActiveAdminEmailsAsync(ct);

        var payload = new Dictionary<string, object>
        {
            ["Reported by"] = reporterName,
            ["Position/Role"] = positionOrRole,
            ["Link"] = link,
            ["Priority"] = normalizedPriority,
            ["Admins' e-mail addresses"] = adminEmails,
            ["Summary"] = dto.Summary.Trim()
        };

        var fileContentJson = JsonSerializer.Serialize(payload, JsonOptions);
        var fileName = $"ticket_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}.json";
        var provider = string.IsNullOrWhiteSpace(dto.CloudProvider) ? "dropbox" : dto.CloudProvider.Trim().ToLowerInvariant();

        var (uploaded, cloudPath, message) = await cloudStorage.UploadJsonFileAsync(
            fileName, fileContentJson, provider, dto.CloudAccessToken);

        logger.LogInformation(
            "Created support ticket {FileName} for {Reporter} ({PositionOrRole}, Priority: {Priority}). Cloud upload: {Uploaded}",
            fileName, reporterName, positionOrRole, normalizedPriority, uploaded);

        if (!uploaded)
        {
            return Error.Validation(
                "SupportTicket.UploadFailed",
                message,
                new Dictionary<string, object>
                {
                    ["fileName"] = fileName,
                    ["success"] = false
                });
        }

        return new SupportTicketResponseDto
        {
            Success = true,
            FileName = fileName,
            FileContentJson = fileContentJson,
            UploadedToCloud = true,
            CloudProvider = provider,
            CloudPath = cloudPath,
            Message = message,
            Payload = payload
        };
    }
}

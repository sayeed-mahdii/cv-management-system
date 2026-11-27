using Recruitov.Application.Dtos;

namespace Recruitov.Application.Interfaces;

public interface ICloudinaryService
{
    bool IsConfigured { get; }
    CloudinarySignatureDto GenerateSignedUploadParams(string folder = "recruitov/avatars");
    Task<bool> DeleteImageAsync(string publicId);
}

public interface ICloudStorageService
{
    Task<(bool Success, string? CloudPath, string Message)> UploadJsonFileAsync(
        string fileName,
        string jsonContent,
        string provider = "dropbox",
        string? userSuppliedToken = null);

    bool HasConfiguredDropboxToken { get; }
    bool HasConfiguredOneDriveToken { get; }
}

public interface IDatabaseHealthService
{
    Task<(bool CanConnect, string? ErrorMessage)> CheckConnectionAsync(CancellationToken ct = default);
}

public record FailedAccessRuleDto(
    int AttributeDefinitionId,
    string AttributeName,
    string ExpectedRule,
    string ActualValue);

public record AccessEvaluationResult(
    bool IsAllowed,
    IReadOnlyList<FailedAccessRuleDto> FailedRules)
{
    public static AccessEvaluationResult Allow() => new(true, []);
    public static AccessEvaluationResult Deny(IReadOnlyList<FailedAccessRuleDto> failed) => new(false, failed);
}

public interface IPositionAccessEvaluator
{
    Task<AccessEvaluationResult> EvaluateAsync(
        int positionId,
        string candidateUserId,
        CancellationToken ct = default);
}

using ErrorOr;
using Recruitov.Application.Dtos;

namespace Recruitov.Application.Interfaces;

public interface IAdminDataService
{
    Task<List<AdminUserListItemDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<ErrorOr<(int AffectedCount, bool IsBlocked)>> BatchBlockUsersAsync(List<string> userIds, bool isBlocked, CancellationToken ct = default);
    Task<ErrorOr<ManageRolesResponse>> UpdateUserRolesAsync(string targetUserId, string callerId, List<string> roles, CancellationToken ct = default);
    Task<ErrorOr<int>> BatchDeleteUsersAsync(List<string> userIds, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteUserAsync(string userId, CancellationToken ct = default);
}

public interface IAttributeDataService
{
    Task<PagedResult<AttributeDto>> GetAttributesAsync(AttributeSearchQuery query, CancellationToken ct = default);
    Task<AttributeDto?> GetAttributeByIdAsync(int id, CancellationToken ct = default);
    Task<ErrorOr<AttributeDto>> CreateAttributeAsync(CreateAttributeDto dto, string? actorName, CancellationToken ct = default);
    Task<ErrorOr<AttributeDto>> UpdateAttributeAsync(int id, UpdateAttributeDto dto, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteAttributeAsync(int id, CancellationToken ct = default);
}

public interface IAuthDataService
{
    Task<CurrentUserDto> GetOrProvisionCurrentUserAsync(string userId, string? email, string? displayName, CancellationToken ct = default);
    Task UpdatePreferencesAsync(string userId, string language, string theme, CancellationToken ct = default);
}

public interface IDashboardDataService
{
    Task<DashboardResponseDto> GetDashboardAsync(CancellationToken ct = default);
}

public interface IPositionDataService
{
    Task<PagedResult<PositionSummaryDto>> GetPositionsAsync(PositionSearchQuery query, CancellationToken ct = default);
    Task<PositionDetailDto?> GetPositionByIdAsync(int id, string? currentUserId, CancellationToken ct = default);
    Task<PositionDetailDto> CreatePositionAsync(CreatePositionDto dto, CancellationToken ct = default);
    Task<ErrorOr<PositionDetailDto>> UpdatePositionAsync(int id, UpdatePositionDto dto, CancellationToken ct = default);
    Task<ErrorOr<PositionDetailDto>> DuplicatePositionAsync(int id, CancellationToken ct = default);
    Task<ErrorOr<List<ApplicantSummaryDto>>> GetApplicantsAsync(int positionId, CancellationToken ct = default);
    Task<ErrorOr<ApplicantDetailDto>> GetApplicantDetailAsync(int positionId, string candidateId, CancellationToken ct = default);
}

public interface IProfileDataService
{
    Task<ProfileResponseDto?> GetProfileAsync(string userId, CancellationToken ct = default);
    Task<ErrorOr<int>> UpdateProfileAsync(string userId, UpdateProfileDto dto, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> RemoveAttributeValueAsync(string userId, int attributeDefinitionId, CancellationToken ct = default);
    Task<ProjectDto> AddProjectAsync(string userId, CreateProjectDto dto, CancellationToken ct = default);
    Task<ErrorOr<ProjectDto>> UpdateProjectAsync(string userId, int projectId, UpdateProjectDto dto, CancellationToken ct = default);
}

public interface ISearchDataService
{
    Task<GlobalSearchResponseDto> SearchAsync(string? query, string? type, CancellationToken ct = default);
}

public interface ISupportTicketDataService
{
    Task<(string ReporterName, string UserRole)> ResolveUserInfoAsync(string? userId, CancellationToken ct = default);
    Task<List<string>> GetActiveAdminEmailsAsync(CancellationToken ct = default);
    Task<string?> GetPositionTitleAsync(int positionId, CancellationToken ct = default);
}

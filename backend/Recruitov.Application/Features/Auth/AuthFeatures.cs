using ErrorOr;
using MediatR;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;

namespace Recruitov.Application.Features.Auth;

public record GetCurrentUserQuery(string UserId, string? Email, string? DisplayName)
    : IRequest<ErrorOr<CurrentUserDto>>;

public class GetCurrentUserHandler(IAuthDataService authData)
    : IRequestHandler<GetCurrentUserQuery, ErrorOr<CurrentUserDto>>
{
    public async Task<ErrorOr<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.UserId))
            return Error.Unauthorized("Auth.Unauthorized", "User is not authenticated.");

        return await authData.GetOrProvisionCurrentUserAsync(request.UserId, request.Email, request.DisplayName, ct);
    }
}

public record UpdatePreferencesResult(string Message);

public record UpdatePreferencesCommand(string UserId, string? PreferredLanguage, string? PreferredTheme)
    : IRequest<ErrorOr<UpdatePreferencesResult>>;

public class UpdatePreferencesHandler(IAuthDataService authData)
    : IRequestHandler<UpdatePreferencesCommand, ErrorOr<UpdatePreferencesResult>>
{
    public async Task<ErrorOr<UpdatePreferencesResult>> Handle(UpdatePreferencesCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.UserId))
            return Error.Unauthorized("Auth.Unauthorized", "User is not authenticated.");

        await authData.UpdatePreferencesAsync(
            request.UserId,
            request.PreferredLanguage ?? "en",
            request.PreferredTheme ?? "light",
            ct);

        return new UpdatePreferencesResult("Preferences updated.");
    }
}

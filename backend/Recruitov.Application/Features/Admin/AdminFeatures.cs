using ErrorOr;
using FluentValidation;
using MediatR;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;

namespace Recruitov.Application.Features.Admin;

// --- Queries ---
public record GetAdminUsersQuery : IRequest<ErrorOr<List<AdminUserListItemDto>>>;

public class GetAdminUsersHandler(IAdminDataService adminData)
    : IRequestHandler<GetAdminUsersQuery, ErrorOr<List<AdminUserListItemDto>>>
{
    public async Task<ErrorOr<List<AdminUserListItemDto>>> Handle(GetAdminUsersQuery request, CancellationToken ct) =>
        await adminData.GetAllUsersAsync(ct);
}

// --- Batch Block Users ---
public record BatchBlockUsersResult(string Message, int AffectedCount, bool IsBlocked);

public record BatchBlockUsersCommand(List<string> UserIds, bool IsBlocked)
    : IRequest<ErrorOr<BatchBlockUsersResult>>;

public class BatchBlockUsersValidator : AbstractValidator<BatchBlockUsersCommand>
{
    public BatchBlockUsersValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user ID must be provided.");
    }
}

public class BatchBlockUsersHandler(IAdminDataService adminData)
    : IRequestHandler<BatchBlockUsersCommand, ErrorOr<BatchBlockUsersResult>>
{
    public async Task<ErrorOr<BatchBlockUsersResult>> Handle(BatchBlockUsersCommand request, CancellationToken ct)
    {
        var result = await adminData.BatchBlockUsersAsync(request.UserIds, request.IsBlocked, ct);
        if (result.IsError) return result.Errors;

        var (count, isBlocked) = result.Value;
        return new BatchBlockUsersResult($"Successfully updated {count} user(s).", count, isBlocked);
    }
}

// --- Update User Roles ---
public record UpdateUserRolesCommand(string TargetUserId, string CallerId, List<string> Roles)
    : IRequest<ErrorOr<ManageRolesResponse>>;

public class UpdateUserRolesHandler(IAdminDataService adminData)
    : IRequestHandler<UpdateUserRolesCommand, ErrorOr<ManageRolesResponse>>
{
    public Task<ErrorOr<ManageRolesResponse>> Handle(UpdateUserRolesCommand request, CancellationToken ct) =>
        adminData.UpdateUserRolesAsync(request.TargetUserId, request.CallerId, request.Roles ?? [], ct);
}

// --- Batch Delete Users ---
public record BatchDeleteUsersResult(string Message, int DeletedCount);

public record BatchDeleteUsersCommand(List<string> UserIds)
    : IRequest<ErrorOr<BatchDeleteUsersResult>>;

public class BatchDeleteUsersValidator : AbstractValidator<BatchDeleteUsersCommand>
{
    public BatchDeleteUsersValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user ID must be provided.");
    }
}

public class BatchDeleteUsersHandler(IAdminDataService adminData)
    : IRequestHandler<BatchDeleteUsersCommand, ErrorOr<BatchDeleteUsersResult>>
{
    public async Task<ErrorOr<BatchDeleteUsersResult>> Handle(BatchDeleteUsersCommand request, CancellationToken ct)
    {
        var result = await adminData.BatchDeleteUsersAsync(request.UserIds, ct);
        if (result.IsError) return result.Errors;

        return new BatchDeleteUsersResult($"Successfully deleted {result.Value} user(s).", result.Value);
    }
}

// --- Delete Single User ---
public record DeleteUserResult(string Message);

public record DeleteUserCommand(string UserId) : IRequest<ErrorOr<DeleteUserResult>>;

public class DeleteUserHandler(IAdminDataService adminData)
    : IRequestHandler<DeleteUserCommand, ErrorOr<DeleteUserResult>>
{
    public async Task<ErrorOr<DeleteUserResult>> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var result = await adminData.DeleteUserAsync(request.UserId, ct);
        if (result.IsError) return result.Errors;

        return new DeleteUserResult("User deleted successfully.");
    }
}

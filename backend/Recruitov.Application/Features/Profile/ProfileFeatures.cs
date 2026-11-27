using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;

namespace Recruitov.Application.Features.Profile;

// --- Get Profile ---
public record GetProfileQuery(string TargetUserId, string CallerId, bool IsRecruiterOrAdmin)
    : IRequest<ErrorOr<ProfileResponseDto>>;

public class GetProfileHandler(IProfileDataService profileData)
    : IRequestHandler<GetProfileQuery, ErrorOr<ProfileResponseDto>>
{
    public async Task<ErrorOr<ProfileResponseDto>> Handle(GetProfileQuery request, CancellationToken ct)
    {
        if (request.CallerId != request.TargetUserId && !request.IsRecruiterOrAdmin)
            return Error.Forbidden("Profile.Forbidden", "You are not allowed to view this profile.");

        var profile = await profileData.GetProfileAsync(request.TargetUserId, ct);
        return profile is null
            ? Error.NotFound("Profile.NotFound", $"User with ID '{request.TargetUserId}' not found.")
            : profile;
    }
}

// --- Update Profile ---
public record UpdateProfileResult(string Message, int Version);

public record UpdateProfileCommand(string TargetUserId, string CallerId, bool IsAdmin, UpdateProfileDto Dto)
    : IRequest<ErrorOr<UpdateProfileResult>>;

public class UpdateProfileHandler(IProfileDataService profileData)
    : IRequestHandler<UpdateProfileCommand, ErrorOr<UpdateProfileResult>>
{
    public async Task<ErrorOr<UpdateProfileResult>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        if (request.CallerId != request.TargetUserId && !request.IsAdmin)
            return Error.Forbidden("Profile.Forbidden", "You are not allowed to update this profile.");

        var result = await profileData.UpdateProfileAsync(request.TargetUserId, request.Dto, ct);
        if (result.IsError) return result.Errors;

        return new UpdateProfileResult("Profile updated successfully.", result.Value);
    }
}

// --- Remove Custom Attribute Value ---
public record RemoveAttributeValueCommand(string TargetUserId, string CallerId, bool IsAdmin, int AttributeDefinitionId)
    : IRequest<ErrorOr<Deleted>>;

public class RemoveAttributeValueHandler(IProfileDataService profileData)
    : IRequestHandler<RemoveAttributeValueCommand, ErrorOr<Deleted>>
{
    public Task<ErrorOr<Deleted>> Handle(RemoveAttributeValueCommand request, CancellationToken ct)
    {
        if (request.CallerId != request.TargetUserId && !request.IsAdmin)
            return Task.FromResult<ErrorOr<Deleted>>(Error.Forbidden("Profile.Forbidden", "Access forbidden."));

        return profileData.RemoveAttributeValueAsync(request.TargetUserId, request.AttributeDefinitionId, ct);
    }
}

// --- Add Project ---
public record AddProjectCommand(string TargetUserId, string CallerId, bool IsAdmin, CreateProjectDto Dto)
    : IRequest<ErrorOr<ProjectDto>>;

public class AddProjectValidator : AbstractValidator<AddProjectCommand>
{
    public AddProjectValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().Length(2, 150);
    }
}

public class AddProjectHandler(IProfileDataService profileData)
    : IRequestHandler<AddProjectCommand, ErrorOr<ProjectDto>>
{
    public async Task<ErrorOr<ProjectDto>> Handle(AddProjectCommand request, CancellationToken ct)
    {
        if (request.CallerId != request.TargetUserId && !request.IsAdmin)
            return Error.Forbidden("Profile.Forbidden", "Access forbidden.");

        return await profileData.AddProjectAsync(request.TargetUserId, request.Dto, ct);
    }
}

// --- Update Project ---
public record UpdateProjectCommand(string TargetUserId, string CallerId, bool IsAdmin, int ProjectId, UpdateProjectDto Dto)
    : IRequest<ErrorOr<ProjectDto>>;

public class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().Length(2, 150);
    }
}

public class UpdateProjectHandler(IProfileDataService profileData)
    : IRequestHandler<UpdateProjectCommand, ErrorOr<ProjectDto>>
{
    public Task<ErrorOr<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken ct)
    {
        if (request.CallerId != request.TargetUserId && !request.IsAdmin)
            return Task.FromResult<ErrorOr<ProjectDto>>(Error.Forbidden("Profile.Forbidden", "Access forbidden."));

        return profileData.UpdateProjectAsync(request.TargetUserId, request.ProjectId, request.Dto, ct);
    }
}

// --- Delete Project (uses IRepository<Project>) ---
public record DeleteProjectCommand(string TargetUserId, string CallerId, bool IsAdmin, int ProjectId)
    : IRequest<ErrorOr<Deleted>>;

public class DeleteProjectHandler(IRepository<Project> projectRepo)
    : IRequestHandler<DeleteProjectCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteProjectCommand request, CancellationToken ct)
    {
        if (request.CallerId != request.TargetUserId && !request.IsAdmin)
            return Error.Forbidden("Profile.Forbidden", "Access forbidden.");

        var project = await projectRepo.FindFirstAsync(
            p => p.Id == request.ProjectId && p.UserId == request.TargetUserId,
            trackChanges: true,
            ct);

        if (project is null)
            return Error.NotFound("Project.NotFound", $"Project with ID {request.ProjectId} not found.");

        projectRepo.Remove(project);
        await projectRepo.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

// --- Get Upload Signature ---
public record GetUploadSignatureQuery : IRequest<ErrorOr<CloudinarySignatureDto>>;

public class GetUploadSignatureHandler(ICloudinaryService cloudinary)
    : IRequestHandler<GetUploadSignatureQuery, ErrorOr<CloudinarySignatureDto>>
{
    public Task<ErrorOr<CloudinarySignatureDto>> Handle(GetUploadSignatureQuery request, CancellationToken ct) =>
        Task.FromResult<ErrorOr<CloudinarySignatureDto>>(cloudinary.GenerateSignedUploadParams("recruitov/avatars"));
}

// --- Withdraw Application By CvId (uses IRepository<Cv>) ---
public record WithdrawApplicationByCvIdCommand(int CvId, string CallerId, bool IsAdmin)
    : IRequest<ErrorOr<Deleted>>;

public class WithdrawApplicationByCvIdHandler(
    IRepository<Cv> cvRepo,
    ILogger<WithdrawApplicationByCvIdHandler> logger)
    : IRequestHandler<WithdrawApplicationByCvIdCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(WithdrawApplicationByCvIdCommand request, CancellationToken ct)
    {
        var cv = await cvRepo.FindFirstAsync(c => c.Id == request.CvId, trackChanges: true, ct);
        if (cv is null)
            return Error.NotFound("Cv.NotFound", $"Application with ID {request.CvId} not found.");

        if (cv.UserId != request.CallerId && !request.IsAdmin)
            return Error.Forbidden("Cv.Forbidden", "You are not allowed to withdraw this application.");

        cvRepo.Remove(cv);
        await cvRepo.SaveChangesAsync(ct);

        logger.LogInformation("Application {CvId} withdrawn by user {UserId}", request.CvId, request.CallerId);
        return Result.Deleted;
    }
}

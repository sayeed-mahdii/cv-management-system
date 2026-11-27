using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;
using Recruitov.Domain.Enums;

namespace Recruitov.Application.Features.Positions;

// --- List Positions ---
public record GetPositionsQuery(PositionSearchQuery Query) : IRequest<ErrorOr<PagedResult<PositionSummaryDto>>>;

public class GetPositionsHandler(IPositionDataService positionData)
    : IRequestHandler<GetPositionsQuery, ErrorOr<PagedResult<PositionSummaryDto>>>
{
    public async Task<ErrorOr<PagedResult<PositionSummaryDto>>> Handle(GetPositionsQuery request, CancellationToken ct) =>
        await positionData.GetPositionsAsync(request.Query, ct);
}

// --- Get Single Position ---
public record GetPositionByIdQuery(int Id, string? CurrentUserId) : IRequest<ErrorOr<PositionDetailDto>>;

public class GetPositionByIdHandler(IPositionDataService positionData)
    : IRequestHandler<GetPositionByIdQuery, ErrorOr<PositionDetailDto>>
{
    public async Task<ErrorOr<PositionDetailDto>> Handle(GetPositionByIdQuery request, CancellationToken ct)
    {
        var position = await positionData.GetPositionByIdAsync(request.Id, request.CurrentUserId, ct);
        return position is null
            ? Error.NotFound("Position.NotFound", $"Position with ID {request.Id} not found.")
            : position;
    }
}

// --- Create Position ---
public record CreatePositionCommand(CreatePositionDto Dto) : IRequest<ErrorOr<PositionDetailDto>>;

public class CreatePositionValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionValidator()
    {
        RuleFor(x => x.Dto.Title).NotEmpty().Length(3, 150);
        RuleFor(x => x.Dto.ShortDescription).NotEmpty().Length(5, 500);
        RuleFor(x => x.Dto.MaxProjects).InclusiveBetween(1, 10);
    }
}

public class CreatePositionHandler(
    IPositionDataService positionData,
    ILogger<CreatePositionHandler> logger)
    : IRequestHandler<CreatePositionCommand, ErrorOr<PositionDetailDto>>
{
    public async Task<ErrorOr<PositionDetailDto>> Handle(CreatePositionCommand request, CancellationToken ct)
    {
        var created = await positionData.CreatePositionAsync(request.Dto, ct);
        logger.LogInformation("Created new position: '{Title}' (ID: {Id})", created.Title, created.Id);
        return created;
    }
}

// --- Update Position ---
public record UpdatePositionCommand(int Id, UpdatePositionDto Dto) : IRequest<ErrorOr<PositionDetailDto>>;

public class UpdatePositionValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionValidator()
    {
        RuleFor(x => x.Dto.Title).NotEmpty().Length(3, 150);
        RuleFor(x => x.Dto.ShortDescription).NotEmpty().Length(5, 500);
        RuleFor(x => x.Dto.MaxProjects).InclusiveBetween(1, 10);
    }
}

public class UpdatePositionHandler(IPositionDataService positionData)
    : IRequestHandler<UpdatePositionCommand, ErrorOr<PositionDetailDto>>
{
    public Task<ErrorOr<PositionDetailDto>> Handle(UpdatePositionCommand request, CancellationToken ct) =>
        positionData.UpdatePositionAsync(request.Id, request.Dto, ct);
}

// --- Delete Position (uses IRepository<Position>) ---
public record DeletePositionCommand(int Id) : IRequest<ErrorOr<Deleted>>;

public class DeletePositionHandler(
    IRepository<Position> positionRepo,
    ILogger<DeletePositionHandler> logger)
    : IRequestHandler<DeletePositionCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeletePositionCommand request, CancellationToken ct)
    {
        var position = await positionRepo.GetByIdAsync(request.Id, trackChanges: true, ct);
        if (position is null)
            return Error.NotFound("Position.NotFound", $"Position with ID {request.Id} not found.");

        positionRepo.Remove(position);
        await positionRepo.SaveChangesAsync(ct);

        logger.LogInformation("Deleted position ID: {Id}", request.Id);
        return Result.Deleted;
    }
}

// --- Duplicate Position ---
public record DuplicatePositionCommand(int Id) : IRequest<ErrorOr<PositionDetailDto>>;

public class DuplicatePositionHandler(IPositionDataService positionData)
    : IRequestHandler<DuplicatePositionCommand, ErrorOr<PositionDetailDto>>
{
    public Task<ErrorOr<PositionDetailDto>> Handle(DuplicatePositionCommand request, CancellationToken ct) =>
        positionData.DuplicatePositionAsync(request.Id, ct);
}

// --- Apply To Position (uses IRepository<Position>, IRepository<Cv>, IPositionAccessEvaluator) ---
public record ApplyToPositionResult(string Message, int CvId);

public record ApplyToPositionCommand(int PositionId, string UserId, bool IsAdmin = false)
    : IRequest<ErrorOr<ApplyToPositionResult>>;

public class ApplyToPositionHandler(
    IRepository<Position> positionRepo,
    IRepository<Cv> cvRepo,
    IPositionAccessEvaluator accessEvaluator,
    ILogger<ApplyToPositionHandler> logger)
    : IRequestHandler<ApplyToPositionCommand, ErrorOr<ApplyToPositionResult>>
{
    public async Task<ErrorOr<ApplyToPositionResult>> Handle(ApplyToPositionCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.UserId))
            return Error.Unauthorized("Auth.Unauthorized", "Authentication required.");

        var positionExists = await positionRepo.AnyAsync(p => p.Id == request.PositionId, ct);
        if (!positionExists)
            return Error.NotFound("Position.NotFound", $"Position with ID {request.PositionId} not found.");

        if (!request.IsAdmin)
        {
            var access = await accessEvaluator.EvaluateAsync(request.PositionId, request.UserId, ct);
            if (!access.IsAllowed)
            {
                var summary = string.Join("; ", access.FailedRules.Select(r =>
                    $"{r.AttributeName}: expected {r.ExpectedRule}, got {r.ActualValue}"));

                return Error.Forbidden(
                    "Position.AccessDenied",
                    $"You do not meet the eligibility criteria for this position ({summary}).",
                    new Dictionary<string, object> { ["failedRules"] = access.FailedRules });
            }
        }

        var alreadyApplied = await cvRepo.AnyAsync(
            c => c.PositionId == request.PositionId && c.UserId == request.UserId, ct);

        if (alreadyApplied)
            return Error.Validation("Cv.AlreadyApplied", "You have already submitted an application for this position.");

        var cv = new Cv
        {
            UserId = request.UserId,
            PositionId = request.PositionId,
            State = CvState.Published,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await cvRepo.AddAsync(cv, ct);
        await cvRepo.SaveChangesAsync(ct);

        logger.LogInformation("Candidate {UserId} applied to position {PositionId}", request.UserId, request.PositionId);
        return new ApplyToPositionResult("Application submitted successfully!", cv.Id);
    }
}

// --- Withdraw Application For Position ---
public record WithdrawPositionApplicationCommand(int PositionId, string UserId) : IRequest<ErrorOr<Deleted>>;

public class WithdrawPositionApplicationHandler(
    IRepository<Cv> cvRepo,
    ILogger<WithdrawPositionApplicationHandler> logger)
    : IRequestHandler<WithdrawPositionApplicationCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(WithdrawPositionApplicationCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.UserId))
            return Error.Unauthorized("Auth.Unauthorized", "Authentication required.");

        var cv = await cvRepo.FindFirstAsync(
            c => c.PositionId == request.PositionId && c.UserId == request.UserId,
            trackChanges: true,
            ct);

        if (cv is null)
            return Error.NotFound("Cv.NotFound", "You do not have an active application for this position.");

        cvRepo.Remove(cv);
        await cvRepo.SaveChangesAsync(ct);

        logger.LogInformation("Candidate {UserId} withdrew application for position {PositionId}", request.UserId, request.PositionId);
        return Result.Deleted;
    }
}

// --- Get Applicants ---
public record GetPositionApplicantsQuery(int PositionId) : IRequest<ErrorOr<List<ApplicantSummaryDto>>>;

public class GetPositionApplicantsHandler(IPositionDataService positionData)
    : IRequestHandler<GetPositionApplicantsQuery, ErrorOr<List<ApplicantSummaryDto>>>
{
    public Task<ErrorOr<List<ApplicantSummaryDto>>> Handle(GetPositionApplicantsQuery request, CancellationToken ct) =>
        positionData.GetApplicantsAsync(request.PositionId, ct);
}

// --- Get Applicant Virtual CV Detail ---
public record GetApplicantDetailQuery(int PositionId, string CandidateId) : IRequest<ErrorOr<ApplicantDetailDto>>;

public class GetApplicantDetailHandler(IPositionDataService positionData)
    : IRequestHandler<GetApplicantDetailQuery, ErrorOr<ApplicantDetailDto>>
{
    public Task<ErrorOr<ApplicantDetailDto>> Handle(GetApplicantDetailQuery request, CancellationToken ct) =>
        positionData.GetApplicantDetailAsync(request.PositionId, request.CandidateId, ct);
}

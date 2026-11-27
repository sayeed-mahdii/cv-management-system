using ErrorOr;
using FluentValidation;
using MediatR;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Enums;

namespace Recruitov.Application.Features.Attributes;

public record GetAttributesQuery(AttributeSearchQuery Query) : IRequest<ErrorOr<PagedResult<AttributeDto>>>;

public class GetAttributesHandler(IAttributeDataService attributeData)
    : IRequestHandler<GetAttributesQuery, ErrorOr<PagedResult<AttributeDto>>>
{
    public async Task<ErrorOr<PagedResult<AttributeDto>>> Handle(GetAttributesQuery request, CancellationToken ct) =>
        await attributeData.GetAttributesAsync(request.Query, ct);
}

public record GetAttributeByIdQuery(int Id) : IRequest<ErrorOr<AttributeDto>>;

public class GetAttributeByIdHandler(IAttributeDataService attributeData)
    : IRequestHandler<GetAttributeByIdQuery, ErrorOr<AttributeDto>>
{
    public async Task<ErrorOr<AttributeDto>> Handle(GetAttributeByIdQuery request, CancellationToken ct)
    {
        var attr = await attributeData.GetAttributeByIdAsync(request.Id, ct);
        return attr is null
            ? Error.NotFound("Attribute.NotFound", $"Attribute with ID {request.Id} not found.")
            : attr;
    }
}

public record CreateAttributeCommand(CreateAttributeDto Dto, string? ActorName)
    : IRequest<ErrorOr<AttributeDto>>;

public class CreateAttributeValidator : AbstractValidator<CreateAttributeCommand>
{
    public CreateAttributeValidator()
    {
        RuleFor(x => x.Dto.Name)
            .NotEmpty().WithMessage("Attribute name is required.")
            .Length(2, 100);

        RuleFor(x => x.Dto.Description)
            .MaximumLength(500);

        RuleFor(x => x.Dto.CategoryId)
            .GreaterThan(0).WithMessage("Selected category does not exist.");

        When(x => x.Dto.DataType == DataType.OneOfMany, () =>
        {
            RuleFor(x => x.Dto.Options)
                .Must(opts => opts is not null && opts.Any(o => !string.IsNullOrWhiteSpace(o)))
                .WithMessage("At least one option is required for a 'One of many' attribute.");
        });
    }
}

public class CreateAttributeHandler(IAttributeDataService attributeData)
    : IRequestHandler<CreateAttributeCommand, ErrorOr<AttributeDto>>
{
    public Task<ErrorOr<AttributeDto>> Handle(CreateAttributeCommand request, CancellationToken ct) =>
        attributeData.CreateAttributeAsync(request.Dto, request.ActorName, ct);
}

public record UpdateAttributeCommand(int Id, UpdateAttributeDto Dto) : IRequest<ErrorOr<AttributeDto>>;

public class UpdateAttributeValidator : AbstractValidator<UpdateAttributeCommand>
{
    public UpdateAttributeValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().Length(2, 100);
        RuleFor(x => x.Dto.Description).MaximumLength(500);
        RuleFor(x => x.Dto.CategoryId).GreaterThan(0);
    }
}

public class UpdateAttributeHandler(IAttributeDataService attributeData)
    : IRequestHandler<UpdateAttributeCommand, ErrorOr<AttributeDto>>
{
    public Task<ErrorOr<AttributeDto>> Handle(UpdateAttributeCommand request, CancellationToken ct) =>
        attributeData.UpdateAttributeAsync(request.Id, request.Dto, ct);
}

public record DeleteAttributeCommand(int Id) : IRequest<ErrorOr<Deleted>>;

public class DeleteAttributeHandler(IAttributeDataService attributeData)
    : IRequestHandler<DeleteAttributeCommand, ErrorOr<Deleted>>
{
    public Task<ErrorOr<Deleted>> Handle(DeleteAttributeCommand request, CancellationToken ct) =>
        attributeData.DeleteAttributeAsync(request.Id, ct);
}

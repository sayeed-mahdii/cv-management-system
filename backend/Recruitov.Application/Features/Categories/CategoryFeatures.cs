using ErrorOr;
using MediatR;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Entities;

namespace Recruitov.Application.Features.Categories;

public record GetCategoriesQuery : IRequest<ErrorOr<List<CategoryDto>>>;

public class GetCategoriesHandler(IRepository<Category> categoryRepo)
    : IRequestHandler<GetCategoriesQuery, ErrorOr<List<CategoryDto>>>
{
    public async Task<ErrorOr<List<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await categoryRepo.FindAsync(_ => true, trackChanges: false, ct);
        return categories
            .OrderBy(c => c.Id)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToList();
    }
}

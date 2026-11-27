using ErrorOr;
using MediatR;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;

namespace Recruitov.Application.Features.Search;

public record GlobalSearchQuery(string? Query, string? Type) : IRequest<ErrorOr<GlobalSearchResponseDto>>;

public class GlobalSearchHandler(ISearchDataService searchData)
    : IRequestHandler<GlobalSearchQuery, ErrorOr<GlobalSearchResponseDto>>
{
    public async Task<ErrorOr<GlobalSearchResponseDto>> Handle(GlobalSearchQuery request, CancellationToken ct) =>
        await searchData.SearchAsync(request.Query, request.Type, ct);
}

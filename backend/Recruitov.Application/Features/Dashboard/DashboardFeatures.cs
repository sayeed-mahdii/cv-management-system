using ErrorOr;
using MediatR;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;

namespace Recruitov.Application.Features.Dashboard;

public record GetDashboardQuery : IRequest<ErrorOr<DashboardResponseDto>>;

public class GetDashboardHandler(IDashboardDataService dashboardData)
    : IRequestHandler<GetDashboardQuery, ErrorOr<DashboardResponseDto>>
{
    public async Task<ErrorOr<DashboardResponseDto>> Handle(GetDashboardQuery request, CancellationToken ct) =>
        await dashboardData.GetDashboardAsync(ct);
}

using ErrorOr;
using MediatR;
using Recruitov.Application.Dtos;
using Recruitov.Application.Interfaces;

namespace Recruitov.Application.Features.Health;

public record GetHealthQuery : IRequest<ErrorOr<HealthStatusDto>>;

public class GetHealthHandler(IDatabaseHealthService healthService)
    : IRequestHandler<GetHealthQuery, ErrorOr<HealthStatusDto>>
{
    public async Task<ErrorOr<HealthStatusDto>> Handle(GetHealthQuery request, CancellationToken ct)
    {
        var (canConnect, error) = await healthService.CheckConnectionAsync(ct);
        return new HealthStatusDto
        {
            Status = canConnect ? "Healthy" : "Degraded",
            Database = canConnect ? "Connected" : "Disconnected",
            Error = error,
            Timestamp = DateTime.UtcNow
        };
    }
}

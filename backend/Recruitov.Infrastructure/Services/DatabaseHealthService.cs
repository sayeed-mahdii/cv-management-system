using Microsoft.Extensions.Logging;
using Recruitov.Application.Interfaces;
using Recruitov.Infrastructure.Data;

namespace Recruitov.Infrastructure.Services;

public class DatabaseHealthService(
    AppDbContext db,
    ILogger<DatabaseHealthService> logger) : IDatabaseHealthService
{
    public async Task<(bool CanConnect, string? ErrorMessage)> CheckConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync(ct);
            return (canConnect, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database connection check failed.");
            return (false, ex.Message);
        }
    }
}

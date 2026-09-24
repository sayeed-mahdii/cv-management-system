using Microsoft.AspNetCore.Mvc;
using backend.Data;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext db, ILogger<HealthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint to verify backend is running and database is reachable.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        bool canConnect = false;
        string? errorMessage = null;

        try
        {
            canConnect = await _db.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection check failed.");
            errorMessage = ex.Message;
        }

        return Ok(new
        {
            status = canConnect ? "Healthy" : "Degraded",
            database = canConnect ? "Connected" : "Disconnected",
            error = errorMessage,
            timestamp = DateTime.UtcNow
        });
    }
}

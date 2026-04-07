using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;

namespace webhook_processing_platform.Api.Controllers
{
  [Route("health")]
  [ApiController]
  public class HealthController : ControllerBase
  {
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<HealthController> _logger;

    public HealthController(NpgsqlDataSource dataSource, ILogger<HealthController> logger)
    {
      _dataSource = dataSource;
      _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Check()
    {
      try
      {
        await using var conn = await _dataSource.OpenConnectionAsync();
        var result = await conn.QuerySingleAsync<int>("SELECT 1");

        _logger.LogInformation("Database health check passed");
        return Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Database health check failed");
        return StatusCode(503, new { status = "unhealthy", database = "disconnected", error = ex.Message });
      }
    }
  }
}

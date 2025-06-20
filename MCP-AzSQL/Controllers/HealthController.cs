// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MCP_Azsql.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<HealthController> _logger;
    private readonly HealthCheckService _healthCheckService;

    public HealthController(
        ISqlConnectionFactory connectionFactory, 
        ILogger<HealthController> logger,
        HealthCheckService healthCheckService)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Basic health check endpoint for Container Apps probes
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check with database connectivity
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                status = healthReport.Status.ToString(),
                timestamp = DateTime.UtcNow,
                checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds
                })
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(503, new { status = "unhealthy", error = ex.Message });
        }
    }    /// <summary>
    /// Readiness probe endpoint for Container Apps (direct route)
    /// </summary>
    [HttpGet("/health/ready")]
    public async Task<IActionResult> ReadyDirect()
    {
        return await Ready();
    }

    /// <summary>
    /// Liveness probe endpoint for Container Apps (direct route)
    /// </summary>
    [HttpGet("/health/live")]
    public IActionResult LiveDirect()
    {
        return Live();
    }

    /// <summary>
    /// Readiness probe endpoint for Container Apps
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            using var connection = await _connectionFactory.GetOpenConnectionAsync();
            return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Readiness check failed");
            return StatusCode(503, new { status = "not ready", error = ex.Message });
        }
    }

    /// <summary>
    /// Liveness probe endpoint for Container Apps
    /// </summary>
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }
}

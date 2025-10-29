using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace TOCC.IBE.Compare.Server.Controllers
{
    /// <summary>
    /// Health check controller for Kubernetes readiness and liveness probes.
    /// </summary>
    [ApiController]
    [Route("v1")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Health check endpoint for Kubernetes readiness and liveness probes.
        /// Returns 200 OK if the application is healthy and ready to serve requests.
        /// </summary>
        /// <returns>Health status response</returns>
        /// <response code="200">Application is healthy and ready</response>
        /// <response code="503">Application is not ready or unhealthy</response>
        [HttpGet("ping")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public IActionResult Ping()
        {
            try
            {
                // Basic health check - you can add more sophisticated checks here
                // such as database connectivity, external service availability, etc.
                
                var response = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    service = "tocc-ibe-compare",
                    version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown"
                };

                _logger.LogDebug("Health check ping successful at {Timestamp}", DateTime.UtcNow);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check ping failed");
                
                var errorResponse = new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    service = "tocc-ibe-compare",
                    error = "Health check failed"
                };
                
                return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
            }
        }

        /// <summary>
        /// Detailed health check endpoint with more comprehensive status information.
        /// </summary>
        /// <returns>Detailed health status</returns>
        /// <response code="200">Application is healthy with detailed status</response>
        /// <response code="503">Application is not ready with detailed error information</response>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public IActionResult Health()
        {
            try
            {
                // More detailed health checks can be added here
                var response = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    service = "tocc-ibe-compare",
                    version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown",
                    uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
                    checks = new
                    {
                        api = "healthy",
                        memory = "healthy",
                        // Add more specific checks as needed
                        // database = CheckDatabaseHealth(),
                        // externalServices = CheckExternalServices()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed");
                
                var errorResponse = new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    service = "tocc-ibe-compare",
                    error = ex.Message,
                    checks = new
                    {
                        api = "unhealthy"
                    }
                };
                
                return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
            }
        }
    }
}

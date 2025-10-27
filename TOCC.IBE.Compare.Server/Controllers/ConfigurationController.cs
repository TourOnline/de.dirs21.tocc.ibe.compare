using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TOCC.IBE.Compare.Server.Models;
using TOCC.IBE.Compare.Server.Services;

namespace TOCC.IBE.Compare.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IntegrationTestSettings _settings;
    private readonly ConfigurationService _configService;

    public ConfigurationController(
        IOptions<IntegrationTestSettings> settings,
        ConfigurationService configService)
    {
        _settings = settings.Value;
        _configService = configService;
    }

    [HttpGet("integration-test")]
    public ActionResult<IntegrationTestSettings> GetIntegrationTestSettings()
    {
        return Ok(_settings);
    }

    [HttpGet("health")]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }
}

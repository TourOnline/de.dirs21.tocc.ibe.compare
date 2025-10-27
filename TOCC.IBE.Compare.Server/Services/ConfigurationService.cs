using Microsoft.Extensions.Configuration;
using TOCC.IBE.Compare.Server.Models;

namespace TOCC.IBE.Compare.Server.Services;

public class ConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IntegrationTestSettings GetIntegrationTestSettings()
    {
        var settings = new IntegrationTestSettings();
        _configuration.GetSection("IntegrationTest").Bind(settings);
        return settings;
    }

    /// <summary>
    /// Builds configuration from appsettings files.
    /// Use ConfigurationHelper.BuildConfiguration() instead for better production support.
    /// </summary>
    [Obsolete("Use ConfigurationHelper.BuildConfiguration() instead")]
    public static IConfiguration BuildConfiguration(string? basePath = null)
    {
        var configBuilder = new ConfigurationBuilder();
        
        if (!string.IsNullOrEmpty(basePath))
        {
            configBuilder.SetBasePath(basePath);
        }
        else
        {
            configBuilder.SetBasePath(AppContext.BaseDirectory);
        }
        
        configBuilder
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return configBuilder.Build();
    }
}

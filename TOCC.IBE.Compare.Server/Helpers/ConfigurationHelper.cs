using Microsoft.Extensions.Configuration;

namespace TOCC.IBE.Compare.Server.Helpers;

/// <summary>
/// Helper class to load configuration from appsettings files.
/// This allows test projects and other consumers to access centralized configuration.
/// Production-ready: Uses environment variables and explicit paths.
/// </summary>
public static class ConfigurationHelper
{
    private const string ConfigPathEnvVar = "TOCC_CONFIG_PATH";
    
    /// <summary>
    /// Builds configuration from appsettings files.
    /// Priority order:
    /// 1. Explicit configPath parameter
    /// 2. TOCC_CONFIG_PATH environment variable
    /// 3. Current directory (AppContext.BaseDirectory)
    /// </summary>
    /// <param name="configPath">Optional explicit path to configuration directory.</param>
    /// <returns>IConfiguration instance with loaded settings</returns>
    public static IConfiguration BuildConfiguration(string? configPath = null)
    {
        string basePath;

        if (!string.IsNullOrEmpty(configPath))
        {
            // Use explicit path
            basePath = configPath;
        }
        else
        {
            // Check environment variable
            var envPath = Environment.GetEnvironmentVariable(ConfigPathEnvVar);
            if (!string.IsNullOrEmpty(envPath))
            {
                basePath = envPath;
            }
            else
            {
                // Fallback to current directory
                basePath = AppContext.BaseDirectory;
            }
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    /// <summary>
    /// Gets a specific configuration value.
    /// </summary>
    public static T? GetValue<T>(string key, T? defaultValue = default)
    {
        var config = BuildConfiguration();
        return config.GetValue<T>(key) ?? defaultValue;
    }

    /// <summary>
    /// Gets a configuration section.
    /// </summary>
    public static IConfigurationSection GetSection(string key)
    {
        var config = BuildConfiguration();
        return config.GetSection(key);
    }
}

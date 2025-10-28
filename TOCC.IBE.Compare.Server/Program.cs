using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TOCC.IBE.Compare.Server.Models;
using TOCC.IBE.Compare.Server.Services;
using TOCC.IBE.Compare.Server.Infrastructure;
using TOCC.IBE.Compare.Models.Services;
using TOCC.IBE.Compare.Models.Common;

// Handle ALL missing assembly references gracefully
// This prevents ReflectionTypeLoadException for transitive dependencies
var missingAssemblies = new HashSet<string>();

AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    var assemblyName = new AssemblyName(args.Name);
    var shortName = assemblyName.Name ?? "Unknown";
    
    // Track missing assemblies (silently - not critical for functionality)
    missingAssemblies.Add(shortName);
    
    // Create an empty assembly to satisfy the reference
    // This prevents TypeLoadException during reflection
    try
    {
        // Return a minimal assembly that satisfies the reference
        var assembly = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
            assemblyName, 
            System.Reflection.Emit.AssemblyBuilderAccess.Run);
        return assembly;
    }
    catch
    {
        // If we can't create a dynamic assembly, return null
        return null;
    }
};

// Also handle type load exceptions during reflection
AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, args) =>
{
    return null;
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Configure controllers to exclude problematic TOCC assemblies from discovery
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // Use Newtonsoft.Json for proper JsonProperty attribute support
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    })
    .ConfigureApplicationPartManager(manager =>
    {
        // Remove TOCC assemblies from controller discovery (except Server which has our controllers)
        var partsToRemove = manager.ApplicationParts
            .Where(part => part.Name.StartsWith("TOCC.") && 
                          part.Name != "TOCC.IBE.Compare.Server")
            .ToList();
        
        foreach (var part in partsToRemove)
        {
            manager.ApplicationParts.Remove(part);
        }
    });
// Swagger disabled for faster builds - uncomment when needed:
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// Register configuration settings
builder.Services.Configure<IntegrationTestSettings>(
    builder.Configuration.GetSection("IntegrationTest"));
builder.Services.AddSingleton<ConfigurationService>();

// Register HTTP client factory for ComparisonService
builder.Services.AddHttpClient();

// Register services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IApiCallEnvelopeBuilder, ApiCallEnvelopeBuilder>();
builder.Services.AddSingleton<ITestCaseGenerator, TestCaseGenerator>();
builder.Services.AddSingleton<IComparisonService, ComparisonService>();

// Configure CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger disabled for faster builds - uncomment when needed:
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

// Map controllers
app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

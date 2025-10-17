using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using TOCC.IBE.Compare;

var host = Host.CreateDefaultBuilder(args)

    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddTransient<Class1>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var class1 = scope.ServiceProvider.GetRequiredService<Class1>();
class1.AddMessage("Hello from DI!");
Console.WriteLine($"Message: {class1.Message}");

var config = host.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
if (string.IsNullOrEmpty(config["IBEV1"]) || string.IsNullOrEmpty(config["IBEV2"]))
{
    Console.WriteLine("Configuration keys IBEV1 or IBEV2 are missing or empty.");
}
else
{
    Console.WriteLine($"IBEV1: {config["IBEV1"]}");
    Console.WriteLine($"IBEV2: {config["IBEV2"]}");
}

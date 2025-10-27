using TOCC.IBE.Compare.Server.Models;
using TOCC.IBE.Compare.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Swagger disabled for faster builds - uncomment when needed:
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// Register configuration settings
builder.Services.Configure<IntegrationTestSettings>(
    builder.Configuration.GetSection("IntegrationTest"));
builder.Services.AddSingleton<ConfigurationService>();

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

app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

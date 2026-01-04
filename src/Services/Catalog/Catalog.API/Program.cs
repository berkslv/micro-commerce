using BuildingBlocks.Messaging.Filters.Correlations;
using BuildingBlocks.Messaging.Filters.Localization;
using BuildingBlocks.Messaging.Filters.Tokens;
using BuildingBlocks.Observability;
using Catalog.API;
using Catalog.API.Middleware;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry observability
builder.AddObservability();

// Add services from all layers
builder.Services.AddCatalogApi(builder.Configuration);
builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

var app = builder.Build();

// Initialize database
await app.InitialiseDatabaseAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Add correlation middleware
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.UseMiddleware<LocalizationMiddleware>();

// Add exception handling middleware
app.UseExceptionHandling();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Catalog.API" }));

try
{
    Log.Information("Starting Catalog.API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Catalog.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Marker class for WebApplicationFactory
public partial class Program { }

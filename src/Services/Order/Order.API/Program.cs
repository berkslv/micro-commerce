using BuildingBlocks.Messaging.Filters.Correlations;
using BuildingBlocks.Messaging.Filters.Localization;
using BuildingBlocks.Messaging.Filters.Tokens;
using BuildingBlocks.Observability;
using Order.API;
using Order.API.Middleware;
using Order.Application;
using Order.Infrastructure;
using Order.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry observability
builder.AddObservability();

// Add services from all layers
builder.Services.AddOrderApi(builder.Configuration);
builder.Services.AddOrderApplication();
builder.Services.AddOrderInfrastructure(builder.Configuration);

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
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Order.API" }));

try
{
    Log.Information("Starting Order.API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Marker class for WebApplicationFactory
public partial class Program { }

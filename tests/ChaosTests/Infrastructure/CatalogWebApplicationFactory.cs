using Catalog.API;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ChaosTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for Catalog API with chaos testing support.
/// Supports configurable connection strings and fault injection.
/// </summary>
public class CatalogWebApplicationFactory : WebApplicationFactory<ICatalogApiMarker>
{
    private readonly string _connectionString;
    private readonly string? _rabbitMqConnectionString;
    private readonly Action<IServiceCollection>? _configureServices;

    public CatalogWebApplicationFactory(
        string connectionString,
        string? rabbitMqConnectionString = null,
        Action<IServiceCollection>? configureServices = null)
    {
        _connectionString = connectionString;
        _rabbitMqConnectionString = rabbitMqConnectionString;
        _configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:CatalogDb", _connectionString);
        
        if (_rabbitMqConnectionString != null)
        {
            builder.UseSetting("ConnectionStrings:RabbitMq", _rabbitMqConnectionString);
        }

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext configuration
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();
            
            // Add test DbContext using connection string
            services.AddDbContext<CatalogDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(_connectionString);
            });

            // Replace the database initializer with a no-op version for testing
            services.RemoveAll<CatalogDbContextInitialiser>();
            services.AddScoped<CatalogDbContextInitialiser, TestCatalogDbContextInitialiser>();

            // Configure MassTransit TestHarness for testing events
            services.AddMassTransitTestHarness(cfg =>
            {
                cfg.SetKebabCaseEndpointNameFormatter();
            });

            // Apply custom service configuration for chaos testing
            _configureServices?.Invoke(services);
        });
    }
}

/// <summary>
/// Test initializer that skips migration (already done by test setup)
/// </summary>
public class TestCatalogDbContextInitialiser : CatalogDbContextInitialiser
{
    public TestCatalogDbContextInitialiser(
        ILogger<CatalogDbContextInitialiser> logger,
        CatalogDbContext context) : base(logger, context)
    {
    }

    public override Task InitialiseAsync() => Task.CompletedTask;

    public override Task SeedAsync() => Task.CompletedTask;
}

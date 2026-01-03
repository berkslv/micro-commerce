using Order.API;
using Order.Infrastructure.Persistence;
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
/// Custom WebApplicationFactory for Order API with chaos testing support.
/// Supports configurable connection strings and fault injection.
/// </summary>
public class OrderWebApplicationFactory : WebApplicationFactory<IOrderApiMarker>
{
    private readonly string _connectionString;
    private readonly string? _rabbitMqConnectionString;
    private readonly Action<IServiceCollection>? _configureServices;

    public OrderWebApplicationFactory(
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
        builder.UseSetting("ConnectionStrings:OrderDb", _connectionString);
        
        if (_rabbitMqConnectionString != null)
        {
            builder.UseSetting("ConnectionStrings:RabbitMq", _rabbitMqConnectionString);
        }

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext configuration
            services.RemoveAll<DbContextOptions<OrderDbContext>>();
            
            // Add test DbContext using connection string
            services.AddDbContext<OrderDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(_connectionString);
            });

            // Replace the database initializer with a no-op version for testing
            services.RemoveAll<OrderDbContextInitialiser>();
            services.AddScoped<OrderDbContextInitialiser, TestOrderDbContextInitialiser>();

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
public class TestOrderDbContextInitialiser : OrderDbContextInitialiser
{
    public TestOrderDbContextInitialiser(
        ILogger<OrderDbContextInitialiser> logger,
        OrderDbContext context) : base(logger, context)
    {
    }

    public override Task InitialiseAsync() => Task.CompletedTask;

    public override Task SeedAsync() => Task.CompletedTask;
}

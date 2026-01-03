using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Persistence;

namespace Order.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:OrderDb", _connectionString);

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

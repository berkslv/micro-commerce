using Catalog.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for Catalog API integration tests.
/// Uses Testcontainers for PostgreSQL and RabbitMQ.
/// </summary>
public class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private RabbitMqContainer _rabbitMqContainer = null!;

    public async Task InitializeAsync()
    {
        // Create PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("catalog_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        // Create RabbitMQ container
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
            .Build();

        // Start containers
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set testing configuration to skip Swagger
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:Enabled"] = "true",
                ["ConnectionStrings:RabbitMq"] = _rabbitMqContainer.GetConnectionString()
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();
            services.RemoveAll<CatalogDbContext>();

            // Add DbContext with Testcontainer PostgreSQL
            services.AddDbContext<CatalogDbContext>((sp, options) =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Configure MassTransit for testing with in-memory harness
            services.RemoveAll<IBusControl>();
            services.RemoveAll<IBus>();
            
            services.AddMassTransitTestHarness(cfg =>
            {
                cfg.SetKebabCaseEndpointNameFormatter();
            });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates a new HttpClient with a fresh database context.
    /// Ensures database is created and migrations are applied.
    /// </summary>
    public HttpClient CreateClientWithDatabase()
    {
        var client = CreateClient();
        
        // Ensure database is created
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        dbContext.Database.EnsureCreated();
        
        return client;
    }

    /// <summary>
    /// Gets the MassTransit test harness for event verification.
    /// </summary>
    public ITestHarness GetTestHarness()
    {
        return Services.GetRequiredService<ITestHarness>();
    }

    /// <summary>
    /// Resets the database by deleting and recreating it.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
}

using Catalog.Infrastructure.Persistence;
using ChaosTests.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChaosTests;

/// <summary>
/// NUnit SetUpFixture that provides shared test infrastructure for chaos tests.
/// </summary>
[SetUpFixture]
public class Testing
{
    private static ITestDatabase _database = null!;
    private static CatalogWebApplicationFactory _catalogFactory = null!;
    private static OrderWebApplicationFactory _orderFactory = null!;
    private static IServiceScopeFactory _catalogScopeFactory = null!;
    private static IServiceScopeFactory _orderScopeFactory = null!;

    public static ITestDatabase Database => _database;
    public static HttpClient CatalogClient { get; private set; } = null!;
    public static HttpClient OrderClient { get; private set; } = null!;
    public static ResilienceMetrics Metrics { get; } = new();

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        _database = new PostgreSqlTestDatabase();
        await _database.InitialiseAsync();

        // Initialize Catalog factory and create schema
        _catalogFactory = new CatalogWebApplicationFactory(
            _database.CatalogConnectionString,
            _database.RabbitMqConnectionString);
        
        _catalogScopeFactory = _catalogFactory.Services.GetRequiredService<IServiceScopeFactory>();
        
        // Initialize database schema for Catalog
        using (var scope = _catalogScopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await context.Database.MigrateAsync();
        }

        // Initialize Order factory and create schema
        _orderFactory = new OrderWebApplicationFactory(
            _database.OrderConnectionString,
            _database.RabbitMqConnectionString);
        
        _orderScopeFactory = _orderFactory.Services.GetRequiredService<IServiceScopeFactory>();
        
        // Initialize database schema for Order
        using (var scope = _orderScopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Order.Infrastructure.Persistence.OrderDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        CatalogClient = _catalogFactory.CreateClient();
        OrderClient = _orderFactory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task RunAfterAllTests()
    {
        CatalogClient?.Dispose();
        OrderClient?.Dispose();
        await _catalogFactory.DisposeAsync();
        await _orderFactory.DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <summary>
    /// Sends a MediatR request through the Catalog service.
    /// </summary>
    public static async Task<TResponse> SendCatalogAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _catalogScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        return await mediator.Send(request);
    }

    /// <summary>
    /// Sends a MediatR request through the Order service.
    /// </summary>
    public static async Task<TResponse> SendOrderAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _orderScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        return await mediator.Send(request);
    }

    /// <summary>
    /// Adds an entity to the Catalog database.
    /// </summary>
    public static async Task<TEntity> AddCatalogEntityAsync<TEntity>(TEntity entity) where TEntity : class
    {
        using var scope = _catalogScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        context.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Adds an entity to the Order database.
    /// </summary>
    public static async Task<TEntity> AddOrderEntityAsync<TEntity>(TEntity entity) where TEntity : class
    {
        using var scope = _orderScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Order.Infrastructure.Persistence.OrderDbContext>();
        context.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Resets metrics between tests.
    /// </summary>
    public static void ResetMetrics()
    {
        Metrics.Reset();
    }
}

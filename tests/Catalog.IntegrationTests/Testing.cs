using Catalog.Infrastructure.Persistence;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

[SetUpFixture]
public class Testing
{
    private static ITestDatabase _database = null!;
    private static CustomWebApplicationFactory _factory = null!;
    private static IServiceScopeFactory _scopeFactory = null!;
    private static ITestHarness _testHarness = null!;

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        _database = new PostgreSqlTestDatabase();
        await _database.InitialiseAsync();

        _factory = new CustomWebApplicationFactory(_database.GetConnectionString());

        _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        _testHarness = _factory.Services.GetRequiredService<ITestHarness>();
        
        await _testHarness.Start();
    }

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        return await mediator.Send(request);
    }

    public static async Task SendAsync(IBaseRequest request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        await mediator.Send(request);
    }

    public static async Task ResetState()
    {
        try
        {
            await _database.ResetAsync();
        }
        catch (Exception)
        {
            // Ignore reset failures
        }
    }

    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await context.FindAsync<TEntity>(keyValues);
    }

    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        context.Add(entity);

        await context.SaveChangesAsync();
    }

    public static async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await context.Set<TEntity>().CountAsync();
    }

    public static HttpClient CreateClient()
    {
        return _factory.CreateClient();
    }

    /// <summary>
    /// Asserts that an event of type T was published.
    /// </summary>
    public static async Task<bool> EventPublished<T>() where T : class
    {
        return await _testHarness.Published.Any<T>();
    }

    /// <summary>
    /// Asserts that an event of type T was published matching the predicate.
    /// </summary>
    public static async Task<bool> EventPublished<T>(Func<T, bool> predicate) where T : class
    {
        return await _testHarness.Published.Any<T>(x => predicate(x.Context.Message));
    }

    /// <summary>
    /// Gets all published events of type T.
    /// </summary>
    public static IEnumerable<T> GetPublishedEvents<T>() where T : class
    {
        return _testHarness.Published.Select<T>().Select(x => x.Context.Message);
    }

    /// <summary>
    /// Gets the test harness for advanced assertions.
    /// </summary>
    public static ITestHarness GetTestHarness() => _testHarness;

    [OneTimeTearDown]
    public async Task RunAfterAnyTests()
    {
        await _testHarness.Stop();
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }
}

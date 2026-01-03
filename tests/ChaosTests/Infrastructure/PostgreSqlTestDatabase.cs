using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace ChaosTests.Infrastructure;

/// <summary>
/// Interface for managing test database containers.
/// </summary>
public interface ITestDatabase
{
    string CatalogConnectionString { get; }
    string OrderConnectionString { get; }
    string RabbitMqConnectionString { get; }
    
    Task InitialiseAsync();
    Task ResetDatabaseAsync();
    Task DisposeAsync();
}

/// <summary>
/// PostgreSQL and RabbitMQ test containers for chaos testing.
/// Provides infrastructure for both Catalog and Order services.
/// </summary>
public class PostgreSqlTestDatabase : ITestDatabase
{
    private PostgreSqlContainer _catalogDbContainer = null!;
    private PostgreSqlContainer _orderDbContainer = null!;
    private RabbitMqContainer _rabbitMqContainer = null!;

    public string CatalogConnectionString => _catalogDbContainer.GetConnectionString();
    public string OrderConnectionString => _orderDbContainer.GetConnectionString();
    public string RabbitMqConnectionString => $"amqp://guest:guest@{_rabbitMqContainer.Hostname}:{_rabbitMqContainer.GetMappedPublicPort(5672)}";

    public async Task InitialiseAsync()
    {
        // Start containers in parallel
        _catalogDbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("catalog_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _orderDbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("order_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .Build();

        await Task.WhenAll(
            _catalogDbContainer.StartAsync(),
            _orderDbContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        );
    }

    public Task ResetDatabaseAsync()
    {
        // For chaos tests, we may want to keep data between tests
        // to verify system recovery
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            _catalogDbContainer.DisposeAsync().AsTask(),
            _orderDbContainer.DisposeAsync().AsTask(),
            _rabbitMqContainer.DisposeAsync().AsTask()
        );
    }
}

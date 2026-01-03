using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Order.Infrastructure.Persistence;
using Respawn;
using Testcontainers.PostgreSql;

namespace Order.IntegrationTests;

public class PostgreSqlTestDatabase : ITestDatabase
{
    private readonly PostgreSqlContainer _container;
    private DbConnection _connection = null!;
    private string _connectionString = null!;
    private Respawner _respawner = null!;

    public PostgreSqlTestDatabase()
    {
        _container = new PostgreSqlBuilder()
            .WithAutoRemove(true)
            .Build();
    }

    public async Task InitialiseAsync()
    {
        await _container.StartAsync();

        // Use the default database from the container directly
        _connectionString = _container.GetConnectionString();

        _connection = new NpgsqlConnection(_connectionString);

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(_connectionString)
            .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var context = new OrderDbContext(options);

        // Create the database schema
        await context.Database.EnsureCreatedAsync();

        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection,
            new RespawnerOptions 
            { 
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = ["__EFMigrationsHistory", "InboxState", "OutboxMessage", "OutboxState"]
            });
        await _connection.CloseAsync();
    }

    public DbConnection GetConnection()
    {
        return _connection;
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }

    public async Task ResetAsync()
    {
        await _connection.OpenAsync();
        await _respawner.ResetAsync(_connection);
        await _connection.CloseAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        // Clear Npgsql connection pool to avoid stale connections
        NpgsqlConnection.ClearAllPools();
        await _container.DisposeAsync();
    }
}

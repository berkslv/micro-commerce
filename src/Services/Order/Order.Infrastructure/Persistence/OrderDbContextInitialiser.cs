using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Domain.Entities;
using Order.Domain.ValueObjects;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Persistence;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<OrderDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class OrderDbContextInitialiser
{
    private readonly ILogger<OrderDbContextInitialiser> _logger;
    private readonly OrderDbContext _context;

    public OrderDbContextInitialiser(
        ILogger<OrderDbContextInitialiser> logger,
        OrderDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public virtual async Task InitialiseAsync()
    {
        try
        {
            _logger.LogInformation("Starting database migration...");
            
            // Check if database can be connected
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogWarning("Cannot connect to database. Skipping migration. Please ensure PostgreSQL is running.");
                return;
            }
            
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public virtual async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        // Seed sample products if they don't exist
        if (!await _context.Products.AnyAsync())
        {
            var products = new[]
            {
                Product.Create(
                    Guid.NewGuid(),
                    "Wireless Mouse",
                    29.99m,
                    "USD",
                    true),
                Product.Create(
                    Guid.NewGuid(),
                    "USB-C Cable",
                    12.99m,
                    "USD",
                    true),
                Product.Create(
                    Guid.NewGuid(),
                    "Bluetooth Headphones",
                    89.99m,
                    "USD",
                    true)
            };

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} products", products.Length);
        }

        // Seed sample orders if they don't exist
        if (!await _context.Orders.AnyAsync())
        {
            var products = await _context.Products.Take(3).ToListAsync();
            
            if (products.Count >= 3)
            {
                // Create sample order 1
                var order1 = OrderEntity.Create(
                    Guid.NewGuid(),
                    "john.doe@example.com",
                    Address.Create(
                        "123 Main Street",
                        "New York",
                        "NY",
                        "USA",
                        "10001"),
                    "Please deliver before 5 PM");

                order1.AddItem(
                    products[0].Id,
                    products[0].Name,
                    Money.Create(products[0].Price, products[0].Currency),
                    2);

                order1.AddItem(
                    products[1].Id,
                    products[1].Name,
                    Money.Create(products[1].Price, products[1].Currency),
                    1);

                // Create sample order 2
                var order2 = OrderEntity.Create(
                    Guid.NewGuid(),
                    "jane.smith@example.com",
                    Address.Create(
                        "456 Oak Avenue",
                        "Los Angeles",
                        "CA",
                        "USA",
                        "90001"),
                    null);

                order2.AddItem(
                    products[2].Id,
                    products[2].Name,
                    Money.Create(products[2].Price, products[2].Currency),
                    1);

                // Create sample order 3
                var order3 = OrderEntity.Create(
                    Guid.NewGuid(),
                    "bob.wilson@example.com",
                    Address.Create(
                        "789 Pine Road",
                        "Chicago",
                        "IL",
                        "USA",
                        "60601"),
                    "Ring doorbell upon arrival");

                order3.AddItem(
                    products[0].Id,
                    products[0].Name,
                    Money.Create(products[0].Price, products[0].Currency),
                    1);

                order3.AddItem(
                    products[1].Id,
                    products[1].Name,
                    Money.Create(products[1].Price, products[1].Currency),
                    3);

                order3.AddItem(
                    products[2].Id,
                    products[2].Name,
                    Money.Create(products[2].Price, products[2].Currency),
                    1);

                await _context.Orders.AddRangeAsync(new[] { order1, order2, order3 });
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} orders", 3);
            }
        }

        _logger.LogInformation("Database seeding completed");
    }
}

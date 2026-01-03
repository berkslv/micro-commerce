using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<CatalogDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class CatalogDbContextInitialiser
{
    private readonly ILogger<CatalogDbContextInitialiser> _logger;
    private readonly CatalogDbContext _context;

    public CatalogDbContextInitialiser(
        ILogger<CatalogDbContextInitialiser> logger,
        CatalogDbContext context)
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
        // Seed default categories if they don't exist
        if (!await _context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                Category.Create("Electronics", "Electronic devices and accessories"),
                Category.Create("Clothing", "Apparel and fashion items"),
                Category.Create("Books", "Books and educational materials"),
                Category.Create("Home & Garden", "Home improvement and garden supplies"),
                Category.Create("Sports & Outdoors", "Sports equipment and outdoor gear")
            };

            await _context.Categories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} categories", categories.Length);
        }

        // Seed sample products if they don't exist
        if (!await _context.Products.AnyAsync())
        {
            var electronicsCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == "Electronics");

            if (electronicsCategory != null)
            {
                var products = new[]
                {
                    Product.Create(
                        ProductName.Create("Wireless Mouse"),
                        "Ergonomic wireless mouse with 2.4GHz connectivity",
                        Money.Create(29.99m, "USD"),
                        100,
                        Sku.Create("MOUSE-001"),
                        electronicsCategory.Id),
                    Product.Create(
                        ProductName.Create("USB-C Cable"),
                        "High-speed USB-C charging cable, 2 meters",
                        Money.Create(12.99m, "USD"),
                        200,
                        Sku.Create("CABLE-001"),
                        electronicsCategory.Id),
                    Product.Create(
                        ProductName.Create("Bluetooth Headphones"),
                        "Noise-cancelling wireless headphones",
                        Money.Create(89.99m, "USD"),
                        50,
                        Sku.Create("HEAD-001"),
                        electronicsCategory.Id)
                };

                await _context.Products.AddRangeAsync(products);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} products", products.Length);
            }
        }
    }
}

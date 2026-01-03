using BuildingBlocks.Messaging.Events;
using Catalog.Application.Features.Stock.Commands.ProcessOrderCreated;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Catalog.UnitTests.Features.Stock.Commands.ProcessOrderCreated;

/// <summary>
/// Test DbContext for ProcessOrderCreated tests that require ChangeTracker functionality.
/// </summary>
internal class TestCatalogDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    public TestCatalogDbContext(DbContextOptions<TestCatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasConversion(
                v => v.Value,
                v => ProductName.Create(v));
            entity.Property(e => e.Sku).HasConversion(
                v => v.Value,
                v => Sku.Create(v));
            entity.OwnsOne(e => e.Price);
        });

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}

public class ProcessOrderCreatedCommandHandlerTests : IDisposable
{
    private readonly TestCatalogDbContext _dbContext;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly ProcessOrderCreatedCommandHandler _handler;

    public ProcessOrderCreatedCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestCatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new TestCatalogDbContext(options);
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _handler = new ProcessOrderCreatedCommandHandler(_dbContext, _mockPublishEndpoint.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task Handle_WithSufficientStock_ShouldReserveStockAndPublishSuccess()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct("Test Product", "SKU-001", categoryId, 100);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var items = new List<OrderItemData>
        {
            new(product.Id, "Test Product", 99.99m, "USD", 10)
        };
        var command = new ProcessOrderCreatedCommand(orderId, correlationId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.FailureReason.Should().BeNull();
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.Is<StockReservedEvent>(e => e.OrderId == orderId && e.CorrelationId == correlationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify stock was reserved
        var updatedProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
        updatedProduct!.StockQuantity.Should().Be(90); // 100 - 10
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ShouldReserveAllStock()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product1 = CreateTestProduct("Product 1", "SKU-001", categoryId, 100);
        var product2 = CreateTestProduct("Product 2", "SKU-002", categoryId, 50);
        _dbContext.Products.AddRange(product1, product2);
        await _dbContext.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var items = new List<OrderItemData>
        {
            new(product1.Id, "Product 1", 99.99m, "USD", 10),
            new(product2.Id, "Product 2", 49.99m, "USD", 5)
        };
        var command = new ProcessOrderCreatedCommand(orderId, correlationId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.FailureReason.Should().BeNull();
        
        // Verify stock was reserved for both products
        var updatedProduct1 = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == product1.Id);
        var updatedProduct2 = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == product2.Id);
        updatedProduct1!.StockQuantity.Should().Be(90); // 100 - 10
        updatedProduct2!.StockQuantity.Should().Be(45); // 50 - 5
    }

    [Fact]
    public async Task Handle_WithProductNotFound_ShouldReturnFailure()
    {
        // Arrange - no products in database
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var nonExistentProductId = Guid.NewGuid();
        var items = new List<OrderItemData>
        {
            new(nonExistentProductId, "Non Existent", 99.99m, "USD", 10)
        };
        var command = new ProcessOrderCreatedCommand(orderId, correlationId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("not found");
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<StockReservationFailedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ShouldReturnFailure()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct("Test Product", "SKU-001", categoryId, 5);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var items = new List<OrderItemData>
        {
            new(product.Id, "Test Product", 99.99m, "USD", 100) // More than available stock
        };
        var command = new ProcessOrderCreatedCommand(orderId, correlationId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("Insufficient stock");
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<StockReservationFailedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify stock was NOT modified
        var unchangedProduct = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
        unchangedProduct!.StockQuantity.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenSecondItemFails_ShouldPublishFailure()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product1 = CreateTestProduct("Product 1", "SKU-001", categoryId, 100);
        var product2 = CreateTestProduct("Product 2", "SKU-002", categoryId, 2); // Insufficient stock
        _dbContext.Products.AddRange(product1, product2);
        await _dbContext.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var items = new List<OrderItemData>
        {
            new(product1.Id, "Product 1", 99.99m, "USD", 10),
            new(product2.Id, "Product 2", 49.99m, "USD", 10) // More than available
        };
        var command = new ProcessOrderCreatedCommand(orderId, correlationId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<StockReservationFailedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldPublishStockReservedEventWithCorrectData()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct("Test Product", "SKU-001", categoryId, 100);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var items = new List<OrderItemData>
        {
            new(product.Id, "Test Product", 99.99m, "USD", 25)
        };
        var command = new ProcessOrderCreatedCommand(orderId, correlationId, items);

        StockReservedEvent? publishedEvent = null;
        _mockPublishEndpoint
            .Setup(x => x.Publish(It.IsAny<StockReservedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => publishedEvent = (StockReservedEvent)e);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.OrderId.Should().Be(orderId);
        publishedEvent.CorrelationId.Should().Be(correlationId);
        publishedEvent.Products.Should().HaveCount(1);
        publishedEvent.Products[0].ProductId.Should().Be(product.Id);
        publishedEvent.Products[0].QuantityReserved.Should().Be(25);
    }

    [Fact]
    public async Task Handle_OnFailure_ShouldPublishStockReservationFailedEvent()
    {
        // Arrange - no products in database
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var nonExistentProductId = Guid.NewGuid();
        var items = new List<OrderItemData>
        {
            new(nonExistentProductId, "Non Existent", 99.99m, "USD", 10)
        };
        var command = new ProcessOrderCreatedCommand(orderId, correlationId, items);

        StockReservationFailedEvent? publishedEvent = null;
        _mockPublishEndpoint
            .Setup(x => x.Publish(It.IsAny<StockReservationFailedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => publishedEvent = (StockReservationFailedEvent)e);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.OrderId.Should().Be(orderId);
        publishedEvent.CorrelationId.Should().Be(correlationId);
        publishedEvent.Reason.Should().NotBeNullOrEmpty();
    }

    #region Helper Methods

    private static Product CreateTestProduct(string name, string sku, Guid categoryId, int stockQuantity)
    {
        return Product.Create(
            ProductName.Create(name),
            "Test Description",
            Money.Create(99.99m, "USD"),
            stockQuantity,
            Sku.Create(sku),
            categoryId);
    }

    #endregion
}

using Catalog.Application.Features.Stock.Commands.ProcessOrderCancelled;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Stock.Commands.ProcessOrderCancelled;

public class ProcessOrderCancelledCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly ProcessOrderCancelledCommandHandler _handler;

    public ProcessOrderCancelledCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new ProcessOrderCancelledCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingProducts_ShouldReleaseStock()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct("Test Product", "SKU-001", categoryId, 100);
        // Reserve some stock first (simulating an order was made)
        product.ReserveStock(50); // StockQuantity becomes 50
        
        var products = new List<Product> { product };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(product.Id, 25)
        };
        var command = new ProcessOrderCancelledCommand(orderId, items);

        var initialStockQuantity = product.StockQuantity; // 50

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ReleasedItemCount.Should().Be(1);
        product.StockQuantity.Should().Be(initialStockQuantity + 25); // 75
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleProducts_ShouldReleaseAllStock()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product1 = CreateTestProduct("Product 1", "SKU-001", categoryId, 100);
        var product2 = CreateTestProduct("Product 2", "SKU-002", categoryId, 50);
        product1.ReserveStock(30); // StockQuantity becomes 70
        product2.ReserveStock(20); // StockQuantity becomes 30
        
        var products = new List<Product> { product1, product2 };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(product1.Id, 15),
            new(product2.Id, 10)
        };
        var command = new ProcessOrderCancelledCommand(orderId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ReleasedItemCount.Should().Be(2);
        product1.StockQuantity.Should().Be(85); // 70 + 15
        product2.StockQuantity.Should().Be(40); // 30 + 10
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ShouldSkipAndNotIncreaseCount()
    {
        // Arrange
        var products = new List<Product>();
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var orderId = Guid.NewGuid();
        var nonExistentProductId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(nonExistentProductId, 10)
        };
        var command = new ProcessOrderCancelledCommand(orderId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ReleasedItemCount.Should().Be(0);
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMixedProducts_ShouldOnlyCountExistingProducts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct("Test Product", "SKU-001", categoryId, 100);
        product.ReserveStock(50); // StockQuantity becomes 50
        
        var products = new List<Product> { product };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var orderId = Guid.NewGuid();
        var nonExistentProductId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(product.Id, 25),
            new(nonExistentProductId, 10)
        };
        var command = new ProcessOrderCancelledCommand(orderId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ReleasedItemCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithEmptyItemsList_ShouldReturnZeroCount()
    {
        // Arrange
        var products = new List<Product>();
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>();
        var command = new ProcessOrderCancelledCommand(orderId, items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ReleasedItemCount.Should().Be(0);
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesOnce()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct("Test Product", "SKU-001", categoryId, 100);
        product.ReserveStock(30); // StockQuantity becomes 70
        
        var products = new List<Product> { product };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(product.Id, 15)
        };
        var command = new ProcessOrderCancelledCommand(orderId, items);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCorrectlyIncreaseStockQuantity()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct("Test Product", "SKU-001", categoryId, 100);
        product.ReserveStock(50); // StockQuantity becomes 50
        
        var products = new List<Product> { product };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(product.Id, 30) // Release 30 items
        };
        var command = new ProcessOrderCancelledCommand(orderId, items);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // ReleaseStock increases StockQuantity: 50 + 30 = 80
        product.StockQuantity.Should().Be(80);
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

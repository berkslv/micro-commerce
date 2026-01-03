using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Stock.Commands.ReserveStock;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Stock.Commands.ReserveStock;

public class ReserveStockCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly ReserveStockCommandHandler _handler;

    public ReserveStockCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new ReserveStockCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithSufficientStock_ShouldReserveAndReturnSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var product = CreateTestProduct(100);
        var productId = product.Id;
        var command = new ReserveStockCommand(productId, Quantity: 10, orderId);

        SetupMockDbContext(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProductId.Should().Be(productId);
        result.OrderId.Should().Be(orderId);
        result.RequestedQuantity.Should().Be(10);
        result.RemainingStock.Should().Be(90);
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ShouldReturnFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var product = CreateTestProduct(5);
        var productId = product.Id;
        var command = new ReserveStockCommand(productId, Quantity: 10, orderId);

        SetupMockDbContext(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNonExistingProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var command = new ReserveStockCommand(nonExistingId, Quantity: 10, orderId);

        SetupMockDbContext();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Product*{nonExistingId}*");
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var product = CreateTestProduct(100);
        var productId = product.Id;
        var command = new ReserveStockCommand(productId, Quantity: 10, orderId);

        SetupMockDbContext(product);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithZeroQuantity_ShouldReturnFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var product = CreateTestProduct(100);
        var productId = product.Id;
        var command = new ReserveStockCommand(productId, Quantity: 0, orderId);

        SetupMockDbContext(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.RemainingStock.Should().Be(100); // Stock unchanged since reservation failed
    }

    #region Helper Methods

    private void SetupMockDbContext(Product? product = null)
    {
        var products = product != null ? new List<Product> { product } : new List<Product>();
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static Product CreateTestProduct(int stockQuantity)
    {
        return Product.Create(
            ProductName.Create("Test Product"),
            "Test Description",
            Money.Create(99.99m, "USD"),
            stockQuantity,
            Sku.Create("TEST-SKU-001"),
            Guid.NewGuid());
    }

    #endregion
}

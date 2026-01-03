using Order.Application.Features.Products.Commands.SyncProductUpdated;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using MockQueryable.Moq;

namespace Order.UnitTests.Features.Products.Commands.SyncProductUpdated;

public class SyncProductUpdatedCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly SyncProductUpdatedCommandHandler _handler;

    public SyncProductUpdatedCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new SyncProductUpdatedCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingProduct_ShouldUpdateAndReturnSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = Product.Create(productId, "Old Name", 50.00m, "USD", true);
        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "Updated Name",
            Price: 149.99m,
            Currency: "EUR",
            IsAvailable: false);

        SetupMockDbContext(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Success.Should().BeTrue();
        existingProduct.Name.Should().Be("Updated Name");
        existingProduct.Price.Should().Be(149.99m);
        existingProduct.Currency.Should().Be("EUR");
        existingProduct.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNonExistingProduct_ShouldCreateProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "New Product",
            Price: 99.99m,
            Currency: "USD",
            IsAvailable: true);

        var mockDbSet = SetupMockDbContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Success.Should().BeTrue();
        mockDbSet.Verify(x => x.Add(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = Product.Create(productId, "Test Product", 50.00m, "USD", true);
        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "Updated Name",
            Price: 99.99m,
            Currency: "USD",
            IsAvailable: true);

        SetupMockDbContext(existingProduct);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToSaveChanges()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = Product.Create(productId, "Test Product", 50.00m, "USD", true);
        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "Updated Name",
            Price: 99.99m,
            Currency: "USD",
            IsAvailable: true);

        SetupMockDbContext(existingProduct);
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(cts.Token), Times.Once);
    }

    #region Helper Methods

    private Mock<Microsoft.EntityFrameworkCore.DbSet<Product>> SetupMockDbContext(Product? product = null)
    {
        var products = product != null ? new List<Product> { product } : new List<Product>();
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return mockDbSet;
    }

    #endregion
}

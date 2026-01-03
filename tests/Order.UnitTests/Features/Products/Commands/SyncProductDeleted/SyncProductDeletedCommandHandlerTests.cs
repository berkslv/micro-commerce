using Order.Application.Features.Products.Commands.SyncProductDeleted;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using MockQueryable.Moq;

namespace Order.UnitTests.Features.Products.Commands.SyncProductDeleted;

public class SyncProductDeletedCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly SyncProductDeletedCommandHandler _handler;

    public SyncProductDeletedCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new SyncProductDeletedCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingProduct_ShouldMarkAsUnavailableAndReturnSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = Product.Create(productId, "Test Product", 99.99m, "USD", true);
        var command = new SyncProductDeletedCommand(productId);

        SetupMockDbContext(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Success.Should().BeTrue();
        existingProduct.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNonExistingProduct_ShouldReturnSuccessWithoutError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new SyncProductDeletedCommand(productId);

        SetupMockDbContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithExistingProduct_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = Product.Create(productId, "Test Product", 99.99m, "USD", true);
        var command = new SyncProductDeletedCommand(productId);

        SetupMockDbContext(existingProduct);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistingProduct_ShouldNotCallSaveChangesAsync()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new SyncProductDeletedCommand(productId);

        SetupMockDbContext();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToSaveChanges()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = Product.Create(productId, "Test Product", 99.99m, "USD", true);
        var command = new SyncProductDeletedCommand(productId);

        SetupMockDbContext(existingProduct);
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(cts.Token), Times.Once);
    }

    #region Helper Methods

    private void SetupMockDbContext(Product? product = null)
    {
        var products = product != null ? new List<Product> { product } : new List<Product>();
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    #endregion
}

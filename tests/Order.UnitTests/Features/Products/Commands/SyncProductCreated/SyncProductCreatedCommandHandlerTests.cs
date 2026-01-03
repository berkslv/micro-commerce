using Order.Application.Features.Products.Commands.SyncProductCreated;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using MockQueryable.Moq;

namespace Order.UnitTests.Features.Products.Commands.SyncProductCreated;

public class SyncProductCreatedCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly SyncProductCreatedCommandHandler _handler;

    public SyncProductCreatedCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new SyncProductCreatedCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithNewProduct_ShouldAddProductAndReturnSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Test Product",
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
    public async Task Handle_WithExistingProduct_ShouldUpdateInsteadOfAdd()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = Product.Create(productId, "Old Name", 50.00m, "USD", true);
        var command = new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "New Name",
            Price: 99.99m,
            Currency: "EUR",
            IsAvailable: false);

        var mockDbSet = SetupMockDbContext(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        mockDbSet.Verify(x => x.Add(It.IsAny<Product>()), Times.Never);
        existingProduct.Name.Should().Be("New Name");
        existingProduct.Price.Should().Be(99.99m);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var command = new SyncProductCreatedCommand(
            ProductId: Guid.NewGuid(),
            Name: "Test Product",
            Price: 99.99m,
            Currency: "USD",
            IsAvailable: true);

        SetupMockDbContext();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToSaveChanges()
    {
        // Arrange
        var command = new SyncProductCreatedCommand(
            ProductId: Guid.NewGuid(),
            Name: "Test Product",
            Price: 99.99m,
            Currency: "USD",
            IsAvailable: true);

        SetupMockDbContext();
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

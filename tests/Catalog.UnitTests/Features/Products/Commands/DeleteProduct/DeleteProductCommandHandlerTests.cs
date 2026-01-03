using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Products.Commands.DeleteProduct;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new DeleteProductCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingProduct_ShouldDeleteProduct()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var productId = existingProduct.Id;
        var command = new DeleteProductCommand(productId);
        var mockDbSet = SetupMockDbContext(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        mockDbSet.Verify(x => x.Remove(It.IsAny<Product>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistingProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var command = new DeleteProductCommand(nonExistingId);
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
        var existingProduct = CreateTestProduct();
        var productId = existingProduct.Id;
        var command = new DeleteProductCommand(productId);
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
        var existingProduct = CreateTestProduct();
        var productId = existingProduct.Id;
        var command = new DeleteProductCommand(productId);
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

    private static Product CreateTestProduct()
    {
        return Product.Create(
            ProductName.Create("Test Product"),
            "Test Description",
            Money.Create(99.99m, "USD"),
            100,
            Sku.Create("TEST-SKU-001"),
            Guid.NewGuid());
    }

    #endregion
}

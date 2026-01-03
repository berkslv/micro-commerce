using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Products.Commands.UpdateProduct;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new UpdateProductCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingProduct_ShouldUpdateAndReturnResponse()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var productId = existingProduct.Id; // Use the actual ID from the created product
        var command = new UpdateProductCommand(
            Id: productId,
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "EUR",
            StockQuantity: 50,
            CategoryId: Guid.NewGuid());

        SetupMockDbContext(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.Price.Should().Be(command.Price);
        result.Currency.Should().Be(command.Currency);
    }

    [Fact]
    public async Task Handle_WithNonExistingProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var command = new UpdateProductCommand(
            Id: nonExistingId,
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "EUR",
            StockQuantity: 50,
            CategoryId: Guid.NewGuid());

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
        var command = new UpdateProductCommand(
            Id: productId,
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "EUR",
            StockQuantity: 50,
            CategoryId: Guid.NewGuid());

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
        var command = new UpdateProductCommand(
            Id: productId,
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "EUR",
            StockQuantity: 50,
            CategoryId: Guid.NewGuid());

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

    private static Product CreateTestProduct()
    {
        return Product.Create(
            ProductName.Create("Original Product"),
            "Original Description",
            Money.Create(99.99m, "USD"),
            100,
            Sku.Create("ORIG-SKU-001"),
            Guid.NewGuid());
    }

    #endregion
}

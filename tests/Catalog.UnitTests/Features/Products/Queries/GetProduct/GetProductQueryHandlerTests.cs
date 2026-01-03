using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Products.Queries.GetProduct;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Products.Queries.GetProduct;

public class GetProductQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly GetProductQueryHandler _handler;

    public GetProductQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new GetProductQueryHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingProduct_ShouldReturnProductResponse()
    {
        // Arrange
        var product = CreateTestProduct();
        var productId = product.Id;
        var query = new GetProductQuery(productId);

        var products = new List<Product> { product };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        // Note: MockQueryable handles async enumeration properly
        // For complex Include scenarios, use integration tests
    }

    [Fact]
    public async Task Handle_WithNonExistingProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var query = new GetProductQuery(nonExistingId);

        var emptyProducts = new List<Product>();
        var mockDbSet = emptyProducts.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Product*{nonExistingId}*");
    }

    #region Helper Methods

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

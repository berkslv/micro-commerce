using Catalog.Application.Features.Products.Queries.GetProducts;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new GetProductsQueryHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithProducts_ShouldReturnAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateTestProduct("Product A", "SKU-A"),
            CreateTestProduct("Product B", "SKU-B"),
            CreateTestProduct("Product C", "SKU-C")
        };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithNoProducts_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyProducts = new List<Product>();
        var mockDbSet = emptyProducts.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnProductsOrderedByName()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateTestProduct("Zebra", "SKU-Z"),
            CreateTestProduct("Apple", "SKU-A"),
            CreateTestProduct("Mango", "SKU-M")
        };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Apple");
        result[1].Name.Should().Be("Mango");
        result[2].Name.Should().Be("Zebra");
    }

    #region Helper Methods

    private static Product CreateTestProduct(string name, string sku)
    {
        return Product.Create(
            ProductName.Create(name),
            "Test Description",
            Money.Create(99.99m, "USD"),
            100,
            Sku.Create(sku),
            Guid.NewGuid());
    }

    #endregion
}

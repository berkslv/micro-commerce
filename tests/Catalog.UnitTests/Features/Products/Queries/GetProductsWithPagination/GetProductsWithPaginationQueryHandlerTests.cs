using Catalog.Application.Features.Products.Queries.GetProductsWithPagination;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Products.Queries.GetProductsWithPagination;

public class GetProductsWithPaginationQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly GetProductsWithPaginationQueryHandler _handler;

    public GetProductsWithPaginationQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new GetProductsWithPaginationQueryHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithProducts_ShouldReturnPaginatedList()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "Electronic devices");
        var products = new List<Product>
        {
            CreateTestProduct("Product A", "SKU-A", categoryId),
            CreateTestProduct("Product B", "SKU-B", categoryId),
            CreateTestProduct("Product C", "SKU-C", categoryId)
        };
        
        foreach (var product in products)
        {
            SetCategory(product, category);
        }
        
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithNoProducts_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var products = new List<Product>();
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPage()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "Electronic devices");
        var products = Enumerable.Range(1, 25)
            .Select(i => CreateTestProduct($"Product {i:D2}", $"SKU-{i:D2}", categoryId))
            .ToList();
        
        foreach (var product in products)
        {
            SetCategory(product, category);
        }
        
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(2, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductsOrderedByName()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "Electronic devices");
        var products = new List<Product>
        {
            CreateTestProduct("Zebra Phone", "SKU-Z", categoryId),
            CreateTestProduct("Apple Watch", "SKU-A", categoryId),
            CreateTestProduct("Mango Tablet", "SKU-M", categoryId)
        };
        
        foreach (var product in products)
        {
            SetCategory(product, category);
        }
        
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items[0].Name.Should().Be("Apple Watch");
        result.Items[1].Name.Should().Be("Mango Tablet");
        result.Items[2].Name.Should().Be("Zebra Phone");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldFilterByName()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "Electronic devices");
        var products = new List<Product>
        {
            CreateTestProduct("iPhone 15", "SKU-IPHONE", categoryId),
            CreateTestProduct("Samsung Galaxy", "SKU-SAMSUNG", categoryId),
            CreateTestProduct("iPhone 14", "SKU-IPHONE14", categoryId)
        };
        
        foreach (var product in products)
        {
            SetCategory(product, category);
        }
        
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(1, 10, "iPhone");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.Name.ToLower().Should().Contain("iphone"));
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldFilterBySku()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "Electronic devices");
        var products = new List<Product>
        {
            CreateTestProduct("Product A", "APPLE-001", categoryId),
            CreateTestProduct("Product B", "SAMSUNG-001", categoryId),
            CreateTestProduct("Product C", "APPLE-002", categoryId)
        };
        
        foreach (var product in products)
        {
            SetCategory(product, category);
        }
        
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(1, 10, "APPLE");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithCategoryId_ShouldFilterByCategory()
    {
        // Arrange
        var categoryId1 = Guid.NewGuid();
        var categoryId2 = Guid.NewGuid();
        var category1 = Category.Create("Electronics", "Electronic devices");
        var category2 = Category.Create("Clothing", "Apparel");
        
        var products = new List<Product>
        {
            CreateTestProduct("Product A", "SKU-A", categoryId1),
            CreateTestProduct("Product B", "SKU-B", categoryId2),
            CreateTestProduct("Product C", "SKU-C", categoryId1)
        };
        
        SetCategory(products[0], category1);
        SetCategory(products[1], category2);
        SetCategory(products[2], category1);
        
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(1, 10, null, categoryId1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.CategoryId.Should().Be(categoryId1));
    }

    [Fact]
    public async Task Handle_WithSearchTermAndCategoryId_ShouldApplyBothFilters()
    {
        // Arrange
        var categoryId1 = Guid.NewGuid();
        var categoryId2 = Guid.NewGuid();
        var category1 = Category.Create("Electronics", "Electronic devices");
        var category2 = Category.Create("Clothing", "Apparel");
        
        var products = new List<Product>
        {
            CreateTestProduct("iPhone 15", "SKU-A", categoryId1),
            CreateTestProduct("iPhone Case", "SKU-B", categoryId2),
            CreateTestProduct("Samsung Galaxy", "SKU-C", categoryId1)
        };
        
        SetCategory(products[0], category1);
        SetCategory(products[1], category2);
        SetCategory(products[2], category1);
        
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(1, 10, "iPhone", categoryId1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("iPhone 15");
    }

    [Fact]
    public async Task Handle_LastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "Electronic devices");
        var products = Enumerable.Range(1, 23)
            .Select(i => CreateTestProduct($"Product {i:D2}", $"SKU-{i:D2}", categoryId))
            .ToList();
        
        foreach (var product in products)
        {
            SetCategory(product, category);
        }
        
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(3, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectResponseFields()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "Electronic devices");
        var product = CreateTestProduct("Test Product", "TEST-SKU", categoryId);
        SetCategory(product, category);
        
        var products = new List<Product> { product };
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);

        var query = new GetProductsWithPaginationQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var item = result.Items.Single();
        item.Id.Should().Be(product.Id);
        item.Name.Should().Be("Test Product");
        item.Description.Should().Be("Test Description");
        item.Price.Should().Be(99.99m);
        item.Currency.Should().Be("USD");
        item.SKU.Should().Be("TEST-SKU");
        item.StockQuantity.Should().Be(100);
        item.CategoryId.Should().Be(categoryId);
        item.CategoryName.Should().Be("Electronics");
    }

    #region Helper Methods

    private static Product CreateTestProduct(string name, string sku, Guid categoryId)
    {
        return Product.Create(
            ProductName.Create(name),
            "Test Description",
            Money.Create(99.99m, "USD"),
            100,
            Sku.Create(sku),
            categoryId);
    }

    private static void SetCategory(Product product, Category category)
    {
        // Use reflection to set the Category backing field (get-only property)
        var categoryField = typeof(Product).GetField("<Category>k__BackingField", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        categoryField?.SetValue(product, category);
    }

    #endregion
}

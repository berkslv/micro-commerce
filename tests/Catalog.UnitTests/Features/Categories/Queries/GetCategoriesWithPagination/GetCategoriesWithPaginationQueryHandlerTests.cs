using Catalog.Application.Features.Categories.Queries.GetCategoriesWithPagination;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Categories.Queries.GetCategoriesWithPagination;

public class GetCategoriesWithPaginationQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly GetCategoriesWithPaginationQueryHandler _handler;

    public GetCategoriesWithPaginationQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new GetCategoriesWithPaginationQueryHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithCategories_ShouldReturnPaginatedList()
    {
        // Arrange
        var categories = new List<Category>
        {
            Category.Create("Electronics", "Electronic devices"),
            Category.Create("Clothing", "Apparel and accessories"),
            Category.Create("Books", "Books and publications")
        };
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithNoCategories_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var categories = new List<Category>();
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery();

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
        var categories = Enumerable.Range(1, 25)
            .Select(i => Category.Create($"Category {i:D2}", $"Description {i}"))
            .ToList();
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery(2, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoriesOrderedByName()
    {
        // Arrange
        var categories = new List<Category>
        {
            Category.Create("Zebra", "Z category"),
            Category.Create("Apple", "A category"),
            Category.Create("Mango", "M category")
        };
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items[0].Name.Should().Be("Apple");
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldFilterByName()
    {
        // Arrange
        var categories = new List<Category>
        {
            Category.Create("Electronics Shop", "Electronic devices"),
            Category.Create("Clothing", "Apparel and accessories"),
            Category.Create("Electronics Store", "Consumer electronics")
        };
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery(1, 10, "Electronics");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(c => c.Name.ToLower().Should().Contain("electronics"));
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldFilterByDescription()
    {
        // Arrange
        var categories = new List<Category>
        {
            Category.Create("Electronics", "Electronic devices and gadgets"),
            Category.Create("Clothing", "Apparel and accessories"),
            Category.Create("Books", "Electronic books included")
        };
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery(1, 10, "Electronic");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldBeCaseInsensitive()
    {
        // Arrange
        var categories = new List<Category>
        {
            Category.Create("Electronics", "Electronic devices"),
            Category.Create("Clothing", "Apparel and accessories")
        };
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery(1, 10, "ELECTRONICS");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task Handle_LastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var categories = Enumerable.Range(1, 23)
            .Select(i => Category.Create($"Category {i:D2}", $"Description {i}"))
            .ToList();
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery(3, 10);

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
        var category = Category.Create("Test Category", "Test Description");
        var categories = new List<Category> { category };
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesWithPaginationQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var item = result.Items.Single();
        item.Id.Should().Be(category.Id);
        item.Name.Should().Be("Test Category");
        item.Description.Should().Be("Test Description");
        item.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

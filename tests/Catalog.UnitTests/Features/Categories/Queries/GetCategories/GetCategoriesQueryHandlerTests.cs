using Catalog.Application.Features.Categories.Queries.GetCategories;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Categories.Queries.GetCategories;

public class GetCategoriesQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly GetCategoriesQueryHandler _handler;

    public GetCategoriesQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new GetCategoriesQueryHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithCategories_ShouldReturnAllCategories()
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

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithNoCategories_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyCategories = new List<Category>();
        var mockDbSet = emptyCategories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
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

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Apple");
        result[1].Name.Should().Be("Mango");
        result[2].Name.Should().Be("Zebra");
    }
}

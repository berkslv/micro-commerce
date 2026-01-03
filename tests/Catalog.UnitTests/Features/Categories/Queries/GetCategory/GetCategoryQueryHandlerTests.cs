using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Queries.GetCategory;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Categories.Queries.GetCategory;

public class GetCategoryQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly GetCategoryQueryHandler _handler;

    public GetCategoryQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new GetCategoryQueryHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingCategory_ShouldReturnCategoryResponse()
    {
        // Arrange
        var existingCategory = Category.Create("Electronics", "Electronic devices");
        var categoryId = existingCategory.Id;
        var query = new GetCategoryQuery(categoryId);

        var categories = new List<Category> { existingCategory };
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Electronics");
        result.Description.Should().Be("Electronic devices");
    }

    [Fact]
    public async Task Handle_WithNonExistingCategory_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var query = new GetCategoryQuery(nonExistingId);

        var emptyCategories = new List<Category>();
        var mockDbSet = emptyCategories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Category*{nonExistingId}*");
    }
}

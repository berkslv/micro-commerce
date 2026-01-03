using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.DeleteCategory;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new DeleteCategoryCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingCategoryNoProducts_ShouldDeleteCategory()
    {
        // Arrange
        var existingCategory = Category.Create("Electronics", "Electronic devices");
        var categoryId = existingCategory.Id;
        var command = new DeleteCategoryCommand(categoryId);
        var mockDbSet = SetupMockDbContext(existingCategory);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mockDbSet.Verify(x => x.Remove(It.IsAny<Category>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistingCategory_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var command = new DeleteCategoryCommand(nonExistingId);
        SetupMockDbContext();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Category*{nonExistingId}*");
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var existingCategory = Category.Create("Electronics", "Electronic devices");
        var categoryId = existingCategory.Id;
        var command = new DeleteCategoryCommand(categoryId);
        SetupMockDbContext(existingCategory);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #region Helper Methods

    private Mock<Microsoft.EntityFrameworkCore.DbSet<Category>> SetupMockDbContext(Category? category = null)
    {
        var categories = category != null ? new List<Category> { category } : new List<Category>();
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return mockDbSet;
    }

    #endregion
}

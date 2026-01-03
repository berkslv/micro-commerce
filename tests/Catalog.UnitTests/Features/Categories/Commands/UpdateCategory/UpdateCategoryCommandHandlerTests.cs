using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.UpdateCategory;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly UpdateCategoryCommandHandler _handler;

    public UpdateCategoryCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new UpdateCategoryCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingCategory_ShouldUpdateAndReturnResponse()
    {
        // Arrange
        var existingCategory = Category.Create("Original", "Original Description");
        var categoryId = existingCategory.Id;
        var command = new UpdateCategoryCommand(
            Id: categoryId,
            Name: "Updated Category",
            Description: "Updated Description");

        SetupMockDbContext(existingCategory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
    }

    [Fact]
    public async Task Handle_WithNonExistingCategory_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var command = new UpdateCategoryCommand(
            Id: nonExistingId,
            Name: "Updated Category",
            Description: "Updated Description");

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
        var existingCategory = Category.Create("Original", "Original Description");
        var categoryId = existingCategory.Id;
        var command = new UpdateCategoryCommand(
            Id: categoryId,
            Name: "Updated Category",
            Description: "Updated Description");

        SetupMockDbContext(existingCategory);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #region Helper Methods

    private void SetupMockDbContext(Category? category = null)
    {
        var categories = category != null ? new List<Category> { category } : new List<Category>();
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    #endregion
}

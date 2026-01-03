using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new CreateCategoryCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateCategoryAndReturnResponse()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            Name: "Electronics",
            Description: "Electronic devices and gadgets");

        var mockDbSet = SetupMockDbContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
    }

    [Fact]
    public async Task Handle_ShouldAddCategoryToDbSet()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            Name: "Electronics",
            Description: "Electronic devices");

        var mockDbSet = SetupMockDbContext();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mockDbSet.Verify(x => x.Add(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            Name: "Electronics",
            Description: "Electronic devices");

        SetupMockDbContext();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToSaveChanges()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            Name: "Electronics",
            Description: "Electronic devices");

        SetupMockDbContext();
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(cts.Token), Times.Once);
    }

    #region Helper Methods

    private Mock<Microsoft.EntityFrameworkCore.DbSet<Category>> SetupMockDbContext()
    {
        var categories = new List<Category>();
        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Categories).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return mockDbSet;
    }

    #endregion
}

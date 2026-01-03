using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MockQueryable.Moq;

namespace Catalog.UnitTests.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new CreateProductCommandHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateProductAndReturnResponse()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupMockDbContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.Price.Should().Be(command.Price);
        result.Currency.Should().Be(command.Currency);
        result.SKU.Should().Be(command.SKU.ToUpperInvariant());
        result.StockQuantity.Should().Be(command.StockQuantity);
        result.CategoryId.Should().Be(command.CategoryId);
    }

    [Fact]
    public async Task Handle_ShouldAddProductToDbSet()
    {
        // Arrange
        var command = CreateValidCommand();
        var mockDbSet = SetupMockDbContext();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mockDbSet.Verify(x => x.Add(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupMockDbContext();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithZeroStock_ShouldCreateProduct()
    {
        // Arrange
        var command = CreateValidCommand() with { StockQuantity = 0 };
        SetupMockDbContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.StockQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToSaveChanges()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupMockDbContext();
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(cts.Token), Times.Once);
    }

    #region Helper Methods

    private Mock<Microsoft.EntityFrameworkCore.DbSet<Product>> SetupMockDbContext()
    {
        var products = new List<Product>();
        var mockDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return mockDbSet;
    }

    private static CreateProductCommand CreateValidCommand()
    {
        return new CreateProductCommand(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "TEST-SKU-001",
            StockQuantity: 100,
            CategoryId: Guid.NewGuid());
    }

    #endregion
}

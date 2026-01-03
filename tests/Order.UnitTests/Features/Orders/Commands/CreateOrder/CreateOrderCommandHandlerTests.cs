using BuildingBlocks.Messaging.Models;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Interfaces;
using MockQueryable.Moq;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.UnitTests.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Correlation _correlation;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _correlation = new Correlation { Id = Guid.NewGuid() };
        _handler = new CreateOrderCommandHandler(_mockDbContext.Object, _correlation);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateOrderAndReturnResponse()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupMockDbContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CustomerId.Should().Be(command.CustomerId);
        result.CustomerEmail.Should().Be(command.CustomerEmail);
        result.Status.Should().Be("Pending");
        result.TotalAmount.Should().Be(199.98m); // 2 items * 99.99
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_ShouldAddOrderToDbSet()
    {
        // Arrange
        var command = CreateValidCommand();
        var mockDbSet = SetupMockDbContext();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mockDbSet.Verify(x => x.Add(It.IsAny<OrderEntity>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalAmountFromItems()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "customer@example.com",
            Street: "123 Main St",
            City: "New York",
            State: "NY",
            Country: "USA",
            ZipCode: "10001",
            Notes: null,
            Items:
            [
                new CreateOrderItemDto(Guid.NewGuid(), "Product 1", 10.00m, "USD", 3), // 30
                new CreateOrderItemDto(Guid.NewGuid(), "Product 2", 25.50m, "USD", 2)  // 51
            ]);

        SetupMockDbContext();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(81.00m);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ShouldCreateOrderWithAllItems()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "customer@example.com",
            Street: "123 Main St",
            City: "New York",
            State: "NY",
            Country: "USA",
            ZipCode: "10001",
            Notes: "Please deliver in the morning",
            Items:
            [
                new CreateOrderItemDto(Guid.NewGuid(), "Product 1", 10.00m, "USD", 1),
                new CreateOrderItemDto(Guid.NewGuid(), "Product 2", 20.00m, "USD", 2),
                new CreateOrderItemDto(Guid.NewGuid(), "Product 3", 30.00m, "USD", 3)
            ]);

        OrderEntity? addedOrder = null;
        var mockDbSet = SetupMockDbContext();
        mockDbSet.Setup(x => x.Add(It.IsAny<OrderEntity>()))
            .Callback<OrderEntity>(order => addedOrder = order);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        addedOrder!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldSubmitOrderWithCorrelationId()
    {
        // Arrange
        var command = CreateValidCommand();

        OrderEntity? addedOrder = null;
        var mockDbSet = SetupMockDbContext();
        mockDbSet.Setup(x => x.Add(It.IsAny<OrderEntity>()))
            .Callback<OrderEntity>(order => addedOrder = order);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        addedOrder!.DomainEvents.Should().NotBeEmpty();
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

    [Fact]
    public async Task Handle_ShouldCreateShippingAddressFromCommand()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "customer@example.com",
            Street: "456 Oak Avenue",
            City: "Los Angeles",
            State: "CA",
            Country: "USA",
            ZipCode: "90001",
            Notes: null,
            Items: [new CreateOrderItemDto(Guid.NewGuid(), "Product", 10.00m, "USD", 1)]);

        OrderEntity? addedOrder = null;
        var mockDbSet = SetupMockDbContext();
        mockDbSet.Setup(x => x.Add(It.IsAny<OrderEntity>()))
            .Callback<OrderEntity>(order => addedOrder = order);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        addedOrder!.ShippingAddress.Street.Should().Be("456 Oak Avenue");
        addedOrder.ShippingAddress.City.Should().Be("Los Angeles");
        addedOrder.ShippingAddress.State.Should().Be("CA");
        addedOrder.ShippingAddress.Country.Should().Be("USA");
        addedOrder.ShippingAddress.ZipCode.Should().Be("90001");
    }

    #region Helper Methods

    private Mock<Microsoft.EntityFrameworkCore.DbSet<OrderEntity>> SetupMockDbContext()
    {
        var orders = new List<OrderEntity>();
        var mockDbSet = orders.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Orders).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return mockDbSet;
    }

    private static CreateOrderCommand CreateValidCommand()
    {
        return new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "customer@example.com",
            Street: "123 Main St",
            City: "New York",
            State: "NY",
            Country: "USA",
            ZipCode: "10001",
            Notes: "Test order notes",
            Items:
            [
                new CreateOrderItemDto(Guid.NewGuid(), "Test Product", 99.99m, "USD", 2)
            ]);
    }

    #endregion
}

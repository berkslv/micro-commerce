using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Models;
using Order.Application.Features.Orders.Commands.MarkStockReservationFailed;
using Order.Application.Interfaces;
using Order.Domain.ValueObjects;
using MockQueryable.Moq;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.UnitTests.Features.Orders.Commands.MarkStockReservationFailed;

public class MarkStockReservationFailedCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Correlation _correlation;
    private readonly MarkStockReservationFailedCommandHandler _handler;

    public MarkStockReservationFailedCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _correlation = new Correlation { Id = Guid.NewGuid() };
        _handler = new MarkStockReservationFailedCommandHandler(_mockDbContext.Object, _correlation);
    }

    [Fact]
    public async Task Handle_WithExistingOrder_ShouldCancelAndReturnResponse()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Submit(_correlation.Id.ToString());
        var orderId = order.Id;
        var command = new MarkStockReservationFailedCommand(orderId, "Insufficient stock");

        SetupMockDbContext(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.Status.Should().Be("Cancelled");
        result.FailureReason.Should().Be("Insufficient stock");
    }

    [Fact]
    public async Task Handle_WithNonExistingOrder_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var command = new MarkStockReservationFailedCommand(nonExistingId, "Test reason");

        SetupMockDbContext();

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Order*{nonExistingId}*");
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Submit(_correlation.Id.ToString());
        var command = new MarkStockReservationFailedCommand(order.Id, "Test reason");

        SetupMockDbContext(order);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToSaveChanges()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Submit(_correlation.Id.ToString());
        var command = new MarkStockReservationFailedCommand(order.Id, "Test reason");

        SetupMockDbContext(order);
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _mockDbContext.Verify(db => db.SaveChangesAsync(cts.Token), Times.Once);
    }

    #region Helper Methods

    private void SetupMockDbContext(OrderEntity? order = null)
    {
        var orders = order != null ? new List<OrderEntity> { order } : new List<OrderEntity>();
        var mockDbSet = orders.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Orders).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static OrderEntity CreateTestOrder()
    {
        var address = Address.Create("123 Main St", "New York", "NY", "USA", "10001");
        var order = OrderEntity.Create(Guid.NewGuid(), "customer@example.com", address, "Test notes");
        order.AddItem(Guid.NewGuid(), "Test Product", Money.Create(99.99m, "USD"), 2);
        return order;
    }

    #endregion
}

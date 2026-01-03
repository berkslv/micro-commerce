using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Orders.Commands.CancelOrder;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Commands.MarkStockReserved;
using Order.Domain.Enums;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.IntegrationTests.Orders.Commands.CancelOrder;

using static Testing;

public class CancelOrderTests
{
    [SetUp]
    public async Task SetUp()
    {
        await ResetState();
    }

    private async Task<CreateOrderResponse> CreateTestOrder()
    {
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "test@example.com",
            Street: "123 Main St",
            City: "New York",
            State: "NY",
            Country: "USA",
            ZipCode: "10001",
            Notes: null,
            Items:
            [
                new CreateOrderItemDto(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Test Product",
                    UnitPrice: 29.99m,
                    Currency: "USD",
                    Quantity: 2)
            ]);

        return await SendAsync(command);
    }

    [Test]
    public async Task ShouldCancelPendingOrder()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        var command = new CancelOrderCommand(orderResult.Id, "Customer request");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderResult.Id);
        result.Status.Should().Be(OrderStatus.Cancelled.ToString());
        result.Reason.Should().Be("Customer request");

        // Verify database
        var order = await FindAsync<OrderEntity>(orderResult.Id);
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Cancelled);
        order.Notes.Should().Contain("Customer request");

        // No OrderCancelledEvent for pending orders (no stock was reserved)
        var eventPublished = await EventPublished<OrderCancelledEvent>(e =>
            e.OrderId == orderResult.Id);

        eventPublished.Should().BeFalse();
    }

    [Test]
    public async Task ShouldCancelConfirmedOrderAndPublishEvent()
    {
        // Arrange - Create order and mark as stock reserved then confirm
        var orderResult = await CreateTestOrder();
        await SendAsync(new MarkStockReservedCommand(orderResult.Id));

        var command = new CancelOrderCommand(orderResult.Id, "Changed my mind");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Cancelled.ToString());

        // Verify database
        var order = await FindAsync<OrderEntity>(orderResult.Id);
        order!.Status.Should().Be(OrderStatus.Cancelled);

        // Assert OrderCancelledEvent was published (stock needs to be restored)
        var eventPublished = await EventPublished<OrderCancelledEvent>(e =>
            e.OrderId == orderResult.Id &&
            e.Reason.Contains("Changed my mind") &&
            e.Items.Count == 1);

        eventPublished.Should().BeTrue();
    }

    [Test]
    public async Task ShouldThrowNotFoundForNonExistentOrder()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.NewGuid(), "Test reason");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldThrowWhenCancellingAlreadyCancelledOrder()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        await SendAsync(new CancelOrderCommand(orderResult.Id, "First cancellation"));

        var command = new CancelOrderCommand(orderResult.Id, "Second cancellation");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<DomainException>();
    }

    [Test]
    public async Task ShouldRequireOrderId()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.Empty, "Test reason");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireReason()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        var command = new CancelOrderCommand(orderResult.Id, "");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldNotExceedMaxReasonLength()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        var longReason = new string('x', 501);
        var command = new CancelOrderCommand(orderResult.Id, longReason);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }
}

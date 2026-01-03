using BuildingBlocks.Common.Exceptions;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Commands.MarkStockReservationFailed;
using Order.Domain.Enums;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.IntegrationTests.Orders.Commands.MarkStockReservationFailed;

using static Testing;

public class MarkStockReservationFailedTests
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
    public async Task ShouldMarkStockReservationFailed()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        var command = new MarkStockReservationFailedCommand(orderResult.Id, "Insufficient stock");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderResult.Id);
        result.Status.Should().Be(OrderStatus.Cancelled.ToString());
        result.FailureReason.Should().Be("Insufficient stock");

        // Verify database
        var order = await FindAsync<OrderEntity>(orderResult.Id);
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Cancelled);
        order.Notes.Should().Contain("Insufficient stock");
    }

    [Test]
    public async Task ShouldThrowNotFoundForNonExistentOrder()
    {
        // Arrange
        var command = new MarkStockReservationFailedCommand(Guid.NewGuid(), "Test reason");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldThrowWhenOrderNotPending()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        
        // First mark as failed
        await SendAsync(new MarkStockReservationFailedCommand(orderResult.Id, "First failure"));
        
        // Try again
        var command = new MarkStockReservationFailedCommand(orderResult.Id, "Second failure");

        // Act & Assert - Order is now Cancelled, not Pending
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<DomainException>();
    }

    [Test]
    public async Task ShouldHandleDifferentFailureReasons()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        var command = new MarkStockReservationFailedCommand(orderResult.Id, "Product no longer available");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.FailureReason.Should().Be("Product no longer available");
        
        var order = await FindAsync<OrderEntity>(orderResult.Id);
        order!.Notes.Should().Contain("Product no longer available");
    }
}

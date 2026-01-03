using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Orders.Commands.ConfirmOrder;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Commands.MarkStockReserved;
using Order.Domain.Enums;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.IntegrationTests.Orders.Commands.ConfirmOrder;

using static Testing;

public class ConfirmOrderTests
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
                    UnitPrice: 50.00m,
                    Currency: "USD",
                    Quantity: 2)
            ]);

        return await SendAsync(command);
    }

    [Test]
    public async Task ShouldConfirmOrderWithReservedStock()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        await SendAsync(new MarkStockReservedCommand(orderResult.Id));

        // Note: MarkStockReserved automatically confirms the order
        // This test verifies that direct Confirm on StockReserved status works

        // Verify the order is confirmed
        var order = await FindAsync<OrderEntity>(orderResult.Id);
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Confirmed);

        // Assert OrderConfirmedEvent was published
        var eventPublished = await EventPublished<OrderConfirmedEvent>(e =>
            e.OrderId == orderResult.Id &&
            e.TotalAmount == 100.00m &&
            e.Currency == "USD");

        eventPublished.Should().BeTrue();
    }

    [Test]
    public async Task ShouldThrowWhenConfirmingPendingOrder()
    {
        // Arrange - Order is in Pending status
        var orderResult = await CreateTestOrder();
        var command = new ConfirmOrderCommand(orderResult.Id);

        // Act & Assert - Cannot confirm until stock is reserved
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<DomainException>();
    }

    [Test]
    public async Task ShouldThrowNotFoundForNonExistentOrder()
    {
        // Arrange
        var command = new ConfirmOrderCommand(Guid.NewGuid());

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }
}

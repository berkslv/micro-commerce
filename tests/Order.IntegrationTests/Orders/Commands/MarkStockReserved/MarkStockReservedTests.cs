using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Commands.MarkStockReserved;
using Order.Domain.Enums;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.IntegrationTests.Orders.Commands.MarkStockReserved;

using static Testing;

public class MarkStockReservedTests
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
                    UnitPrice: 25.00m,
                    Currency: "USD",
                    Quantity: 4)
            ]);

        return await SendAsync(command);
    }

    [Test]
    public async Task ShouldMarkStockReservedAndConfirm()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        var command = new MarkStockReservedCommand(orderResult.Id);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderResult.Id);
        result.Status.Should().Be(OrderStatus.Confirmed.ToString());

        // Verify database - order should be Confirmed (auto-confirms after stock reserved)
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
    public async Task ShouldThrowNotFoundForNonExistentOrder()
    {
        // Arrange
        var command = new MarkStockReservedCommand(Guid.NewGuid());

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldThrowWhenOrderNotPending()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        
        // First mark as reserved and confirm
        await SendAsync(new MarkStockReservedCommand(orderResult.Id));
        
        // Try to mark as reserved again
        var command = new MarkStockReservedCommand(orderResult.Id);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<DomainException>();
    }
}

using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Queries.GetOrdersByCustomer;
using Order.Domain.Enums;

namespace Order.IntegrationTests.Orders.Queries.GetOrdersByCustomer;

using static Testing;

public class GetOrdersByCustomerTests
{
    [SetUp]
    public async Task SetUp()
    {
        await ResetState();
    }

    [Test]
    public async Task ShouldReturnOrdersForCustomer()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Create first order
        await SendAsync(new CreateOrderCommand(
            CustomerId: customerId,
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
                    ProductName: "Product A",
                    UnitPrice: 29.99m,
                    Currency: "USD",
                    Quantity: 1)
            ]));

        // Create second order
        await SendAsync(new CreateOrderCommand(
            CustomerId: customerId,
            CustomerEmail: "test@example.com",
            Street: "456 Oak Ave",
            City: "Los Angeles",
            State: "CA",
            Country: "USA",
            ZipCode: "90001",
            Notes: null,
            Items:
            [
                new CreateOrderItemDto(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Product B",
                    UnitPrice: 49.99m,
                    Currency: "USD",
                    Quantity: 2)
            ]));

        var query = new GetOrdersByCustomerQuery(customerId);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.Status.Should().Be(OrderStatus.Pending.ToString()));
    }

    [Test]
    public async Task ShouldReturnOrdersInDescendingOrderByDate()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Create orders with small delays to ensure different timestamps
        var order1 = await SendAsync(new CreateOrderCommand(
            CustomerId: customerId,
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
                    ProductName: "First Product",
                    UnitPrice: 10.00m,
                    Currency: "USD",
                    Quantity: 1)
            ]));

        await Task.Delay(10); // Small delay to ensure different timestamps

        var order2 = await SendAsync(new CreateOrderCommand(
            CustomerId: customerId,
            CustomerEmail: "test@example.com",
            Street: "456 Oak Ave",
            City: "Los Angeles",
            State: "CA",
            Country: "USA",
            ZipCode: "90001",
            Notes: null,
            Items:
            [
                new CreateOrderItemDto(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Second Product",
                    UnitPrice: 20.00m,
                    Currency: "USD",
                    Quantity: 1)
            ]));

        var query = new GetOrdersByCustomerQuery(customerId);

        // Act
        var result = await SendAsync(query);

        // Assert - Most recent order should be first
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(order2.Id);
        result[1].Id.Should().Be(order1.Id);
    }

    [Test]
    public async Task ShouldReturnEmptyListForCustomerWithNoOrders()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetOrdersByCustomerQuery(customerId);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldOnlyReturnOrdersForSpecificCustomer()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();

        // Create order for customer 1
        await SendAsync(new CreateOrderCommand(
            CustomerId: customerId1,
            CustomerEmail: "customer1@example.com",
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
                    ProductName: "Product A",
                    UnitPrice: 29.99m,
                    Currency: "USD",
                    Quantity: 1)
            ]));

        // Create order for customer 2
        await SendAsync(new CreateOrderCommand(
            CustomerId: customerId2,
            CustomerEmail: "customer2@example.com",
            Street: "456 Oak Ave",
            City: "Los Angeles",
            State: "CA",
            Country: "USA",
            ZipCode: "90001",
            Notes: null,
            Items:
            [
                new CreateOrderItemDto(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Product B",
                    UnitPrice: 49.99m,
                    Currency: "USD",
                    Quantity: 1)
            ]));

        var query = new GetOrdersByCustomerQuery(customerId1);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].TotalAmount.Should().Be(29.99m);
    }

    [Test]
    public async Task ShouldReturnOrderAmountsAndCurrency()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        await SendAsync(new CreateOrderCommand(
            CustomerId: customerId,
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
                    ProductName: "Expensive Product",
                    UnitPrice: 100.00m,
                    Currency: "USD",
                    Quantity: 5)
            ]));

        var query = new GetOrdersByCustomerQuery(customerId);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].TotalAmount.Should().Be(500.00m);
        result[0].Currency.Should().Be("USD");
    }
}

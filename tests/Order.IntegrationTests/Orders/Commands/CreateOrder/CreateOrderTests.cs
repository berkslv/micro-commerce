using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Domain.Enums;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.IntegrationTests.Orders.Commands.CreateOrder;

using static Testing;

public class CreateOrderTests
{
    [SetUp]
    public async Task SetUp()
    {
        await ResetState();
    }

    [Test]
    public async Task ShouldCreateOrder()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "test@example.com",
            Street: "123 Main St",
            City: "New York",
            State: "NY",
            Country: "USA",
            ZipCode: "10001",
            Notes: "Test order",
            Items:
            [
                new CreateOrderItemDto(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Test Product",
                    UnitPrice: 29.99m,
                    Currency: "USD",
                    Quantity: 2)
            ]);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CustomerId.Should().Be(command.CustomerId);
        result.CustomerEmail.Should().Be(command.CustomerEmail);
        result.Status.Should().Be(OrderStatus.Pending.ToString());
        result.TotalAmount.Should().Be(59.98m); // 29.99 * 2
        result.Currency.Should().Be("USD");

        // Verify database
        var order = await FindAsync<OrderEntity>(result.Id);
        order.Should().NotBeNull();
        order!.CustomerId.Should().Be(command.CustomerId);
        order.CustomerEmail.Should().Be(command.CustomerEmail);
        order.Status.Should().Be(OrderStatus.Pending);

        // Assert OrderCreatedEvent was published
        var eventPublished = await EventPublished<OrderCreatedEvent>(e =>
            e.OrderId == result.Id &&
            e.CustomerId == command.CustomerId &&
            e.TotalAmount == 59.98m &&
            e.Currency == "USD" &&
            e.Items.Count == 1);

        eventPublished.Should().BeTrue();
    }

    [Test]
    public async Task ShouldCreateOrderWithMultipleItems()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "multi@example.com",
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
                    ProductName: "Product A",
                    UnitPrice: 10.00m,
                    Currency: "USD",
                    Quantity: 3),
                new CreateOrderItemDto(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Product B",
                    UnitPrice: 25.00m,
                    Currency: "USD",
                    Quantity: 2)
            ]);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(80.00m); // (10 * 3) + (25 * 2)

        // Assert OrderCreatedEvent was published with correct items
        var eventPublished = await EventPublished<OrderCreatedEvent>(e =>
            e.OrderId == result.Id &&
            e.Items.Count == 2 &&
            e.TotalAmount == 80.00m);

        eventPublished.Should().BeTrue();
    }

    [Test]
    public async Task ShouldRequireCustomerId()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.Empty,
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
                    Quantity: 1)
            ]);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireValidEmail()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "invalid-email",
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
                    Quantity: 1)
            ]);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireAtLeastOneItem()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "test@example.com",
            Street: "123 Main St",
            City: "New York",
            State: "NY",
            Country: "USA",
            ZipCode: "10001",
            Notes: null,
            Items: []);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireStreet()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "test@example.com",
            Street: "",
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
                    Quantity: 1)
            ]);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireItemProductId()
    {
        // Arrange
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
                    ProductId: Guid.Empty,
                    ProductName: "Test Product",
                    UnitPrice: 29.99m,
                    Currency: "USD",
                    Quantity: 1)
            ]);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequirePositiveQuantity()
    {
        // Arrange
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
                    Quantity: 0)
            ]);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequirePositiveUnitPrice()
    {
        // Arrange
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
                    UnitPrice: -10.00m,
                    Currency: "USD",
                    Quantity: 1)
            ]);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }
}

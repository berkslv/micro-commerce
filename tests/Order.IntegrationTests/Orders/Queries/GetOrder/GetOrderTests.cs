using BuildingBlocks.Common.Exceptions;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Queries.GetOrder;
using Order.Application.Features.Products.Commands.SyncProductCreated;
using Order.Domain.Enums;

namespace Order.IntegrationTests.Orders.Queries.GetOrder;

using static Testing;

public class GetOrderTests
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
            Notes: "Test notes",
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
    public async Task ShouldReturnOrder()
    {
        // Arrange
        var orderResult = await CreateTestOrder();
        var query = new GetOrderQuery(orderResult.Id);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(orderResult.Id);
        result.CustomerId.Should().Be(orderResult.CustomerId);
        result.CustomerEmail.Should().Be(orderResult.CustomerEmail);
        result.Status.Should().Be(OrderStatus.Pending.ToString());
        result.TotalAmount.Should().Be(59.98m);
        result.Currency.Should().Be("USD");
        result.Notes.Should().Be("Test notes");
        result.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task ShouldReturnOrderWithItems()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var createCommand = new CreateOrderCommand(
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
                    ProductId: productId1,
                    ProductName: "Product A",
                    UnitPrice: 10.00m,
                    Currency: "USD",
                    Quantity: 3),
                new CreateOrderItemDto(
                    ProductId: productId2,
                    ProductName: "Product B",
                    UnitPrice: 25.00m,
                    Currency: "USD",
                    Quantity: 2)
            ]);

        var orderResult = await SendAsync(createCommand);
        var query = new GetOrderQuery(orderResult.Id);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalAmount.Should().Be(80.00m); // (10 * 3) + (25 * 2)

        var item1 = result.Items.First(i => i.ProductId == productId1);
        item1.ProductName.Should().Be("Product A");
        item1.UnitPrice.Should().Be(10.00m);
        item1.Quantity.Should().Be(3);
        item1.TotalPrice.Should().Be(30.00m);

        var item2 = result.Items.First(i => i.ProductId == productId2);
        item2.ProductName.Should().Be("Product B");
        item2.UnitPrice.Should().Be(25.00m);
        item2.Quantity.Should().Be(2);
        item2.TotalPrice.Should().Be(50.00m);
    }

    [Test]
    public async Task ShouldIncludeProductAvailabilityFromSyncedProducts()
    {
        // Arrange - Sync a product first
        var productId = Guid.NewGuid();
        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Synced Product",
            Price: 49.99m,
            Currency: "USD",
            IsAvailable: true));

        var createCommand = new CreateOrderCommand(
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
                    ProductId: productId,
                    ProductName: "Synced Product",
                    UnitPrice: 49.99m,
                    Currency: "USD",
                    Quantity: 1)
            ]);

        var orderResult = await SendAsync(createCommand);
        var query = new GetOrderQuery(orderResult.Id);

        // Act
        var result = await SendAsync(query);

        // Assert
        var item = result.Items.First();
        item.IsProductAvailable.Should().BeTrue();
        item.CurrentProductPrice.Should().Be(49.99m);
    }

    [Test]
    public async Task ShouldReturnShippingAddress()
    {
        // Arrange
        var createCommand = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "test@example.com",
            Street: "789 Pine Blvd",
            City: "Chicago",
            State: "IL",
            Country: "USA",
            ZipCode: "60601",
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

        var orderResult = await SendAsync(createCommand);
        var query = new GetOrderQuery(orderResult.Id);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShippingAddress.Should().Contain("789 Pine Blvd");
        result.ShippingAddress.Should().Contain("Chicago");
        result.ShippingAddress.Should().Contain("IL");
        result.ShippingAddress.Should().Contain("USA");
        result.ShippingAddress.Should().Contain("60601");
    }

    [Test]
    public async Task ShouldThrowNotFoundForNonExistentOrder()
    {
        // Arrange
        var query = new GetOrderQuery(Guid.NewGuid());

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<NotFoundException>();
    }
}

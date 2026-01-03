using Order.Application.Features.Products.Commands.SyncProductCreated;
using Order.Application.Features.Products.Commands.SyncProductUpdated;
using Order.Domain.Entities;

namespace Order.IntegrationTests.Products.Commands.SyncProductUpdated;

using static Testing;

public class SyncProductUpdatedTests
{
    [SetUp]
    public async Task SetUp()
    {
        await ResetState();
    }

    [Test]
    public async Task ShouldUpdateExistingProduct()
    {
        // Arrange - Create product first
        var productId = Guid.NewGuid();
        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Original Product",
            Price: 29.99m,
            Currency: "USD",
            IsAvailable: true));

        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "Updated Product",
            Price: 39.99m,
            Currency: "EUR",
            IsAvailable: false);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Success.Should().BeTrue();

        // Verify database
        var product = await FindAsync<Product>(productId);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Updated Product");
        product.Price.Should().Be(39.99m);
        product.Currency.Should().Be("EUR");
        product.IsAvailable.Should().BeFalse();
    }

    [Test]
    public async Task ShouldCreateProductIfNotExists()
    {
        // Arrange - Product doesn't exist yet
        var productId = Guid.NewGuid();
        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "New Product via Update",
            Price: 49.99m,
            Currency: "USD",
            IsAvailable: true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        // Verify database - product should be created
        var product = await FindAsync<Product>(productId);
        product.Should().NotBeNull();
        product!.Name.Should().Be("New Product via Update");
        product.Price.Should().Be(49.99m);
    }

    [Test]
    public async Task ShouldUpdatePrice()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Product",
            Price: 10.00m,
            Currency: "USD",
            IsAvailable: true));

        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "Product",
            Price: 15.00m,
            Currency: "USD",
            IsAvailable: true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        var product = await FindAsync<Product>(productId);
        product!.Price.Should().Be(15.00m);
    }

    [Test]
    public async Task ShouldUpdateAvailability()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Product",
            Price: 10.00m,
            Currency: "USD",
            IsAvailable: true));

        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "Product",
            Price: 10.00m,
            Currency: "USD",
            IsAvailable: false);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        var product = await FindAsync<Product>(productId);
        product!.IsAvailable.Should().BeFalse();
    }

    [Test]
    public async Task ShouldUpdateName()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Old Name",
            Price: 10.00m,
            Currency: "USD",
            IsAvailable: true));

        var command = new SyncProductUpdatedCommand(
            ProductId: productId,
            Name: "New Name",
            Price: 10.00m,
            Currency: "USD",
            IsAvailable: true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        var product = await FindAsync<Product>(productId);
        product!.Name.Should().Be("New Name");
    }
}

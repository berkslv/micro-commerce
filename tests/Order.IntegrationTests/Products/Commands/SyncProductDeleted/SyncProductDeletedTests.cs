using Order.Application.Features.Products.Commands.SyncProductCreated;
using Order.Application.Features.Products.Commands.SyncProductDeleted;
using Order.Domain.Entities;

namespace Order.IntegrationTests.Products.Commands.SyncProductDeleted;

using static Testing;

public class SyncProductDeletedTests
{
    [SetUp]
    public async Task SetUp()
    {
        await ResetState();
    }

    [Test]
    public async Task ShouldMarkProductAsUnavailable()
    {
        // Arrange - Create product first
        var productId = Guid.NewGuid();
        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Product to Delete",
            Price: 29.99m,
            Currency: "USD",
            IsAvailable: true));

        var command = new SyncProductDeletedCommand(productId);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Success.Should().BeTrue();

        // Verify database - product should be marked as unavailable, not deleted
        var product = await FindAsync<Product>(productId);
        product.Should().NotBeNull();
        product!.IsAvailable.Should().BeFalse();
        product.Name.Should().Be("Product to Delete"); // Name preserved for historical orders
    }

    [Test]
    public async Task ShouldSucceedForNonExistentProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new SyncProductDeletedCommand(productId);

        // Act
        var result = await SendAsync(command);

        // Assert - Should succeed even if product doesn't exist
        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task ShouldPreserveProductDataForHistoricalOrders()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Historical Product",
            Price: 99.99m,
            Currency: "USD",
            IsAvailable: true));

        var command = new SyncProductDeletedCommand(productId);

        // Act
        await SendAsync(command);

        // Assert - Product data preserved
        var product = await FindAsync<Product>(productId);
        product!.Name.Should().Be("Historical Product");
        product.Price.Should().Be(99.99m);
        product.Currency.Should().Be("USD");
        product.IsAvailable.Should().BeFalse();
    }

    [Test]
    public async Task ShouldHandleMultipleDeletes()
    {
        // Arrange
        var productId = Guid.NewGuid();
        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Product",
            Price: 10.00m,
            Currency: "USD",
            IsAvailable: true));

        // Act - Delete twice
        await SendAsync(new SyncProductDeletedCommand(productId));
        var result = await SendAsync(new SyncProductDeletedCommand(productId));

        // Assert - Should succeed without errors
        result.Success.Should().BeTrue();

        var product = await FindAsync<Product>(productId);
        product!.IsAvailable.Should().BeFalse();
    }
}

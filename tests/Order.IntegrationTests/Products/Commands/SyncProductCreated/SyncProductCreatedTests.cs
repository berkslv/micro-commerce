using Order.Application.Features.Products.Commands.SyncProductCreated;
using Order.Domain.Entities;

namespace Order.IntegrationTests.Products.Commands.SyncProductCreated;

using static Testing;

public class SyncProductCreatedTests
{
    [SetUp]
    public async Task SetUp()
    {
        await ResetState();
    }

    [Test]
    public async Task ShouldCreateProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "New Product",
            Price: 49.99m,
            Currency: "USD",
            IsAvailable: true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Success.Should().BeTrue();

        // Verify database
        var product = await FindAsync<Product>(productId);
        product.Should().NotBeNull();
        product!.Name.Should().Be("New Product");
        product.Price.Should().Be(49.99m);
        product.Currency.Should().Be("USD");
        product.IsAvailable.Should().BeTrue();
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

        // Update with new data
        var command = new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Updated Product",
            Price: 39.99m,
            Currency: "EUR",
            IsAvailable: false);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        // Verify database - should be updated
        var product = await FindAsync<Product>(productId);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Updated Product");
        product.Price.Should().Be(39.99m);
        product.Currency.Should().Be("EUR");
        product.IsAvailable.Should().BeFalse();
    }

    [Test]
    public async Task ShouldCreateProductWithZeroPrice()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Free Product",
            Price: 0m,
            Currency: "USD",
            IsAvailable: true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        var product = await FindAsync<Product>(productId);
        product!.Price.Should().Be(0m);
    }

    [Test]
    public async Task ShouldCreateUnavailableProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new SyncProductCreatedCommand(
            ProductId: productId,
            Name: "Out of Stock Product",
            Price: 99.99m,
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
    public async Task ShouldHandleMultipleProducts()
    {
        // Arrange & Act
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId1,
            Name: "Product One",
            Price: 10.00m,
            Currency: "USD",
            IsAvailable: true));

        await SendAsync(new SyncProductCreatedCommand(
            ProductId: productId2,
            Name: "Product Two",
            Price: 20.00m,
            Currency: "USD",
            IsAvailable: true));

        // Assert - verify both products exist with correct data
        var product1 = await FindAsync<Product>(productId1);
        var product2 = await FindAsync<Product>(productId2);

        product1.Should().NotBeNull();
        product2.Should().NotBeNull();
        product1!.Name.Should().Be("Product One");
        product2!.Name.Should().Be("Product Two");
    }
}

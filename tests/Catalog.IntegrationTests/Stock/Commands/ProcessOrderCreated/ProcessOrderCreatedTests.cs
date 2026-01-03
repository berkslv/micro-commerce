using BuildingBlocks.Messaging.Events;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Stock.Commands.ProcessOrderCreated;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Stock.Commands.ProcessOrderCreated;

using static Testing;

public class ProcessOrderCreatedTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReserveStockForSingleItem()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "POC-001",
            50,
            categoryResult.Id));

        var orderId = Guid.NewGuid();
        var items = new List<OrderItemData>
        {
            new(productResult.Id, "Test Product", 99.99m, "USD", 10)
        };

        var result = await SendAsync(new ProcessOrderCreatedCommand(
            orderId,
            Guid.NewGuid().ToString(),
            items));

        result.Success.Should().BeTrue();
        result.FailureReason.Should().BeNull();

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(40); // 50 - 10
    }

    [Test]
    public async Task ShouldReserveStockForMultipleItems()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var product1Result = await SendAsync(new CreateProductCommand(
            "Product 1",
            "First product",
            99.99m,
            "USD",
            "POC-002",
            50,
            categoryResult.Id));

        var product2Result = await SendAsync(new CreateProductCommand(
            "Product 2",
            "Second product",
            149.99m,
            "USD",
            "POC-003",
            30,
            categoryResult.Id));

        var orderId = Guid.NewGuid();
        var items = new List<OrderItemData>
        {
            new(product1Result.Id, "Product 1", 99.99m, "USD", 10),
            new(product2Result.Id, "Product 2", 149.99m, "USD", 5)
        };

        var result = await SendAsync(new ProcessOrderCreatedCommand(
            orderId,
            Guid.NewGuid().ToString(),
            items));

        result.Success.Should().BeTrue();

        var product1 = await FindAsync<Product>(product1Result.Id);
        var product2 = await FindAsync<Product>(product2Result.Id);

        product1.Should().NotBeNull();
        product1!.StockQuantity.Should().Be(40); // 50 - 10

        product2.Should().NotBeNull();
        product2!.StockQuantity.Should().Be(25); // 30 - 5
    }

    [Test]
    public async Task ShouldFailWhenProductNotFound()
    {
        var orderId = Guid.NewGuid();
        var items = new List<OrderItemData>
        {
            new(Guid.NewGuid(), "Non-existent Product", 99.99m, "USD", 10)
        };

        var result = await SendAsync(new ProcessOrderCreatedCommand(
            orderId,
            Guid.NewGuid().ToString(),
            items));

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("not found");
    }

    [Test]
    public async Task ShouldFailWhenInsufficientStock()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "POC-004",
            5, // Only 5 in stock
            categoryResult.Id));

        var orderId = Guid.NewGuid();
        var items = new List<OrderItemData>
        {
            new(productResult.Id, "Test Product", 99.99m, "USD", 10) // Try to reserve 10
        };

        var result = await SendAsync(new ProcessOrderCreatedCommand(
            orderId,
            Guid.NewGuid().ToString(),
            items));

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("Insufficient stock");

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(5); // Stock unchanged
    }

    [Test]
    public async Task ShouldRollbackWhenSecondItemFails()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var product1Result = await SendAsync(new CreateProductCommand(
            "Product 1",
            "First product",
            99.99m,
            "USD",
            "POC-005",
            50,
            categoryResult.Id));

        var product2Result = await SendAsync(new CreateProductCommand(
            "Product 2",
            "Second product",
            149.99m,
            "USD",
            "POC-006",
            3, // Only 3 in stock
            categoryResult.Id));

        var orderId = Guid.NewGuid();
        var items = new List<OrderItemData>
        {
            new(product1Result.Id, "Product 1", 99.99m, "USD", 10), // This should succeed
            new(product2Result.Id, "Product 2", 149.99m, "USD", 5)  // This should fail (only 3 available)
        };

        var result = await SendAsync(new ProcessOrderCreatedCommand(
            orderId,
            Guid.NewGuid().ToString(),
            items));

        result.Success.Should().BeFalse();

        // Both products should have original stock (rollback)
        var product1 = await FindAsync<Product>(product1Result.Id);
        var product2 = await FindAsync<Product>(product2Result.Id);

        product1.Should().NotBeNull();
        product1!.StockQuantity.Should().Be(50); // Rolled back

        product2.Should().NotBeNull();
        product2!.StockQuantity.Should().Be(3); // Unchanged
    }

    [Test]
    public async Task ShouldHandleEmptyOrderItems()
    {
        var orderId = Guid.NewGuid();
        var items = new List<OrderItemData>();

        var result = await SendAsync(new ProcessOrderCreatedCommand(
            orderId,
            Guid.NewGuid().ToString(),
            items));

        result.Success.Should().BeTrue();
    }
}

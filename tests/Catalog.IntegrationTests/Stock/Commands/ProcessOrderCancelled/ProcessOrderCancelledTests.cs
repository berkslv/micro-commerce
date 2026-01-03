using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Stock.Commands.ProcessOrderCancelled;
using Catalog.Application.Features.Stock.Commands.ReserveStock;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Stock.Commands.ProcessOrderCancelled;

using static Testing;

public class ProcessOrderCancelledTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReleaseStockForSingleItem()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "PCN-001",
            50,
            categoryResult.Id));

        // Reserve stock first
        await SendAsync(new ReserveStockCommand(
            productResult.Id,
            10,
            Guid.NewGuid()));

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(productResult.Id, 10)
        };

        var result = await SendAsync(new ProcessOrderCancelledCommand(orderId, items));

        result.ReleasedItemCount.Should().Be(1);

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(50); // Back to original
    }

    [Test]
    public async Task ShouldReleaseStockForMultipleItems()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var product1Result = await SendAsync(new CreateProductCommand(
            "Product 1",
            "First product",
            99.99m,
            "USD",
            "PCN-002",
            50,
            categoryResult.Id));

        var product2Result = await SendAsync(new CreateProductCommand(
            "Product 2",
            "Second product",
            149.99m,
            "USD",
            "PCN-003",
            30,
            categoryResult.Id));

        // Reserve stock for both products
        await SendAsync(new ReserveStockCommand(product1Result.Id, 10, Guid.NewGuid()));
        await SendAsync(new ReserveStockCommand(product2Result.Id, 5, Guid.NewGuid()));

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(product1Result.Id, 10),
            new(product2Result.Id, 5)
        };

        var result = await SendAsync(new ProcessOrderCancelledCommand(orderId, items));

        result.ReleasedItemCount.Should().Be(2);

        var product1 = await FindAsync<Product>(product1Result.Id);
        var product2 = await FindAsync<Product>(product2Result.Id);

        product1.Should().NotBeNull();
        product1!.StockQuantity.Should().Be(50); // Back to original

        product2.Should().NotBeNull();
        product2!.StockQuantity.Should().Be(30); // Back to original
    }

    [Test]
    public async Task ShouldHandleNonExistentProduct()
    {
        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(Guid.NewGuid(), 10) // Non-existent product
        };

        var result = await SendAsync(new ProcessOrderCancelledCommand(orderId, items));

        result.ReleasedItemCount.Should().Be(0); // No products released
    }

    [Test]
    public async Task ShouldHandleMixedExistentAndNonExistentProducts()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "PCN-004",
            50,
            categoryResult.Id));

        // Reserve stock
        await SendAsync(new ReserveStockCommand(productResult.Id, 10, Guid.NewGuid()));

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(productResult.Id, 10),
            new(Guid.NewGuid(), 5) // Non-existent product
        };

        var result = await SendAsync(new ProcessOrderCancelledCommand(orderId, items));

        result.ReleasedItemCount.Should().Be(1); // Only one product released

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(50); // Released successfully
    }

    [Test]
    public async Task ShouldHandleEmptyItems()
    {
        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>();

        var result = await SendAsync(new ProcessOrderCancelledCommand(orderId, items));

        result.ReleasedItemCount.Should().Be(0);
    }

    [Test]
    public async Task ShouldReleaseStockEvenWithoutPriorReservation()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "PCN-005",
            50,
            categoryResult.Id));

        var orderId = Guid.NewGuid();
        var items = new List<CancelledItemData>
        {
            new(productResult.Id, 10) // Release without prior reservation
        };

        var result = await SendAsync(new ProcessOrderCancelledCommand(orderId, items));

        result.ReleasedItemCount.Should().Be(1);

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(60); // 50 + 10
    }
}

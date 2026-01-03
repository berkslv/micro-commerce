using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Stock.Commands.ReleaseStock;
using Catalog.Application.Features.Stock.Commands.ReserveStock;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Stock.Commands.ReleaseStock;

using static Testing;

public class ReleaseStockTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidProductId()
    {
        var command = new ReleaseStockCommand(
            Guid.NewGuid(),
            5,
            Guid.NewGuid());

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldReleaseStock()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "REL-001",
            50,
            categoryResult.Id));

        // First reserve some stock
        await SendAsync(new ReserveStockCommand(
            productResult.Id,
            20,
            Guid.NewGuid()));

        // Then release it
        var orderId = Guid.NewGuid();
        var result = await SendAsync(new ReleaseStockCommand(
            productResult.Id,
            20,
            orderId));

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(50); // Back to original
    }

    [Test]
    public async Task ShouldReleasePartialStock()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "REL-002",
            50,
            categoryResult.Id));

        // Reserve stock
        await SendAsync(new ReserveStockCommand(
            productResult.Id,
            30,
            Guid.NewGuid()));

        // Release only part of it
        var result = await SendAsync(new ReleaseStockCommand(
            productResult.Id,
            10,
            Guid.NewGuid()));

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(30); // 50 - 30 + 10 = 30
    }

    [Test]
    public async Task ShouldIncreaseStockOnRelease()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "REL-003",
            10,
            categoryResult.Id));

        // Release stock (adds to existing)
        var result = await SendAsync(new ReleaseStockCommand(
            productResult.Id,
            15,
            Guid.NewGuid()));

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(25); // 10 + 15
    }

    [Test]
    public async Task ShouldReleaseStockMultipleTimes()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "REL-004",
            0, // Start with zero
            categoryResult.Id));

        // Release multiple times
        await SendAsync(new ReleaseStockCommand(productResult.Id, 10, Guid.NewGuid()));
        await SendAsync(new ReleaseStockCommand(productResult.Id, 20, Guid.NewGuid()));
        await SendAsync(new ReleaseStockCommand(productResult.Id, 5, Guid.NewGuid()));

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(35); // 0 + 10 + 20 + 5
    }
}

using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Stock.Commands.ReserveStock;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Stock.Commands.ReserveStock;

using static Testing;

public class ReserveStockTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidProductId()
    {
        var command = new ReserveStockCommand(
            Guid.NewGuid(),
            5,
            Guid.NewGuid());

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldReserveStock()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "RES-001",
            50,
            categoryResult.Id));

        var orderId = Guid.NewGuid();
        var quantityToReserve = 10;

        var result = await SendAsync(new ReserveStockCommand(
            productResult.Id,
            quantityToReserve,
            orderId));

        result.Success.Should().BeTrue();

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(40); // 50 - 10
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
            "RES-002",
            5, // Only 5 in stock
            categoryResult.Id));

        var orderId = Guid.NewGuid();

        var result = await SendAsync(new ReserveStockCommand(
            productResult.Id,
            10, // Try to reserve 10
            orderId));

        result.Success.Should().BeFalse();

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(5); // Stock unchanged
    }

    [Test]
    public async Task ShouldReserveEntireStock()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "RES-003",
            20,
            categoryResult.Id));

        var orderId = Guid.NewGuid();

        var result = await SendAsync(new ReserveStockCommand(
            productResult.Id,
            20, // Reserve all
            orderId));

        result.Success.Should().BeTrue();

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(0);
    }

    [Test]
    public async Task ShouldFailWhenReservingZeroQuantity()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "RES-004",
            50,
            categoryResult.Id));

        var orderId = Guid.NewGuid();

        var result = await SendAsync(new ReserveStockCommand(
            productResult.Id,
            0, // Zero quantity
            orderId));

        result.Success.Should().BeFalse();

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(50); // Stock unchanged
    }

    [Test]
    public async Task ShouldReserveStockMultipleTimes()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "RES-005",
            100,
            categoryResult.Id));

        // First reservation
        var result1 = await SendAsync(new ReserveStockCommand(
            productResult.Id,
            30,
            Guid.NewGuid()));

        result1.Success.Should().BeTrue();

        // Second reservation
        var result2 = await SendAsync(new ReserveStockCommand(
            productResult.Id,
            40,
            Guid.NewGuid()));

        result2.Success.Should().BeTrue();

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(30); // 100 - 30 - 40
    }
}

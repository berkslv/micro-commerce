using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Products.Commands.UpdateProduct;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Products.Commands.UpdateProduct;

using static Testing;

public class UpdateProductTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidProductId()
    {
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            "Updated Description",
            199.99m,
            "USD",
            20,
            Guid.NewGuid());

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Test Category",
            "Test Description"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Original Product",
            "Original Description",
            99.99m,
            "USD",
            "UPD-001",
            10,
            categoryResult.Id));

        var command = new UpdateProductCommand(
            productResult.Id,
            "", // Invalid empty name
            "Updated Description",
            199.99m,
            "USD",
            20,
            categoryResult.Id);

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldUpdateProduct()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Original Smartphone",
            "Original description",
            799.99m,
            "USD",
            "UPD-002",
            30,
            categoryResult.Id));

        var command = new UpdateProductCommand(
            productResult.Id,
            "Updated Smartphone Pro",
            "Updated description with new features",
            999.99m,
            "EUR",
            30,
            categoryResult.Id);

        await SendAsync(command);

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.Name.Value.Should().Be(command.Name);
        product.Description.Should().Be(command.Description);
        product.Price.Amount.Should().Be(command.Price);
        product.Price.Currency.Should().Be(command.Currency);
        product.ModifiedAt.Should().NotBeNull();
        product.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Assert ProductUpdatedEvent was published
        var eventPublished = await EventPublished<ProductUpdatedEvent>(e =>
            e.ProductId == productResult.Id &&
            e.Name == command.Name &&
            e.Price == command.Price &&
            e.Currency == command.Currency);

        eventPublished.Should().BeTrue();
    }

    [Test]
    public async Task ShouldUpdateProductPrice()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "UPD-003",
            10,
            categoryResult.Id));

        var command = new UpdateProductCommand(
            productResult.Id,
            "Test Product",
            "Test description",
            149.99m, // New price
            "USD",
            10,
            categoryResult.Id);

        await SendAsync(command);

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.Price.Amount.Should().Be(149.99m);
    }

    [Test]
    public async Task ShouldUpdateProductCurrency()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            "UPD-004",
            10,
            categoryResult.Id));

        var command = new UpdateProductCommand(
            productResult.Id,
            "Test Product",
            "Test description",
            99.99m,
            "EUR", // Changed currency
            10,
            categoryResult.Id);

        await SendAsync(command);

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.Price.Currency.Should().Be("EUR");
    }

    [Test]
    public async Task ShouldNotChangeSkuOnUpdate()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var originalSku = "UPD-005";
        var productResult = await SendAsync(new CreateProductCommand(
            "Test Product",
            "Test description",
            99.99m,
            "USD",
            originalSku,
            10,
            categoryResult.Id));

        var command = new UpdateProductCommand(
            productResult.Id,
            "Updated Product",
            "Updated description",
            149.99m,
            "EUR",
            20,
            categoryResult.Id);

        await SendAsync(command);

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().NotBeNull();
        product!.Sku.Value.Should().Be(originalSku); // SKU should remain unchanged
    }
}

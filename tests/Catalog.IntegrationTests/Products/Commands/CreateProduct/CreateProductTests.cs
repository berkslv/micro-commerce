using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Products.Commands.CreateProduct;

using static Testing;

public class CreateProductTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateProductCommand(
            "",
            "",
            0,
            "",
            "",
            0,
            Guid.Empty);

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireValidName()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Test Category",
            "Test Description"));

        var command = new CreateProductCommand(
            "A", // Too short
            "Description",
            99.99m,
            "USD",
            "TEST-001",
            10,
            categoryResult.Id);

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequirePositivePrice()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Test Category",
            "Test Description"));

        var command = new CreateProductCommand(
            "Valid Product Name",
            "Description",
            0, // Invalid price
            "USD",
            "TEST-002",
            10,
            categoryResult.Id);

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireValidSKU()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Test Category",
            "Test Description"));

        var command = new CreateProductCommand(
            "Valid Product Name",
            "Description",
            99.99m,
            "USD",
            "AB", // Too short
            10,
            categoryResult.Id);

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRejectNegativeStockQuantity()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Test Category",
            "Test Description"));

        var command = new CreateProductCommand(
            "Valid Product Name",
            "Description",
            99.99m,
            "USD",
            "TEST-003",
            -5, // Negative stock
            categoryResult.Id);

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldCreateProduct()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var command = new CreateProductCommand(
            "Smartphone",
            "A powerful smartphone",
            999.99m,
            "USD",
            "PHONE-001",
            50,
            categoryResult.Id);

        var result = await SendAsync(command);

        var product = await FindAsync<Product>(result.Id);

        product.Should().NotBeNull();
        product!.Name.Value.Should().Be(command.Name);
        product.Description.Should().Be(command.Description);
        product.Price.Amount.Should().Be(command.Price);
        product.Price.Currency.Should().Be(command.Currency);
        product.Sku.Value.Should().Be(command.SKU);
        product.StockQuantity.Should().Be(command.StockQuantity);
        product.CategoryId.Should().Be(command.CategoryId);
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Assert ProductCreatedEvent was published
        var eventPublished = await EventPublished<ProductCreatedEvent>(e =>
            e.ProductId == result.Id &&
            e.Name == command.Name &&
            e.Price == command.Price &&
            e.Currency == command.Currency &&
            e.StockQuantity == command.StockQuantity &&
            e.CategoryId == command.CategoryId &&
            e.IsAvailable == true);

        eventPublished.Should().BeTrue();
    }

    [Test]
    public async Task ShouldCreateProductWithZeroStock()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var command = new CreateProductCommand(
            "Out of Stock Product",
            "Currently unavailable",
            199.99m,
            "EUR",
            "OOS-001",
            0,
            categoryResult.Id);

        var result = await SendAsync(command);

        var product = await FindAsync<Product>(result.Id);

        product.Should().NotBeNull();
        product!.StockQuantity.Should().Be(0);
    }

    [Test]
    public async Task ShouldCreateProductWithDifferentCurrency()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Books",
            "All kinds of books"));

        var command = new CreateProductCommand(
            "Programming Book",
            "Learn to code",
            49.99m,
            "EUR",
            "BOOK-001",
            100,
            categoryResult.Id);

        var result = await SendAsync(command);

        var product = await FindAsync<Product>(result.Id);

        product.Should().NotBeNull();
        product!.Price.Currency.Should().Be("EUR");
    }
}

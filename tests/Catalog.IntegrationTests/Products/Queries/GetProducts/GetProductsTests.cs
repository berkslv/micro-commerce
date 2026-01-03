using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Products.Queries.GetProducts;

namespace Catalog.IntegrationTests.Products.Queries.GetProducts;

using static Testing;

public class GetProductsTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyListWhenNoProducts()
    {
        var query = new GetProductsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldReturnAllProducts()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        await SendAsync(new CreateProductCommand("Product 1", "Desc 1", 99.99m, "USD", "SKU-001", 10, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Product 2", "Desc 2", 149.99m, "USD", "SKU-002", 20, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Product 3", "Desc 3", 199.99m, "USD", "SKU-003", 30, categoryResult.Id));

        var query = new GetProductsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Test]
    public async Task ShouldReturnProductsOrderedByName()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        await SendAsync(new CreateProductCommand("Zebra Product", "Desc", 99.99m, "USD", "SKU-Z01", 10, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Apple Product", "Desc", 149.99m, "USD", "SKU-A01", 20, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Mango Product", "Desc", 199.99m, "USD", "SKU-M01", 30, categoryResult.Id));

        var query = new GetProductsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Apple Product");
        result[1].Name.Should().Be("Mango Product");
        result[2].Name.Should().Be("Zebra Product");
    }

    [Test]
    public async Task ShouldReturnProductsWithCategoryName()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        await SendAsync(new CreateProductCommand("Test Product", "Desc", 99.99m, "USD", "SKU-T01", 10, categoryResult.Id));

        var query = new GetProductsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].CategoryName.Should().Be("Electronics");
    }

    [Test]
    public async Task ShouldReturnProductsFromMultipleCategories()
    {
        var electronicsCategory = await SendAsync(new CreateCategoryCommand("Electronics", "Electronic devices"));
        var booksCategory = await SendAsync(new CreateCategoryCommand("Books", "All kinds of books"));

        await SendAsync(new CreateProductCommand("Smartphone", "Phone", 999.99m, "USD", "SKU-E01", 10, electronicsCategory.Id));
        await SendAsync(new CreateProductCommand("Programming Book", "Book", 49.99m, "USD", "SKU-B01", 50, booksCategory.Id));

        var query = new GetProductsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.CategoryName == "Electronics");
        result.Should().Contain(p => p.CategoryName == "Books");
    }

    [Test]
    public async Task ShouldReturnProductDetails()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Detailed Product",
            "Full description",
            299.99m,
            "EUR",
            "SKU-D01",
            75,
            categoryResult.Id));

        var query = new GetProductsQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var product = result.First();
        product.Id.Should().Be(productResult.Id);
        product.Name.Should().Be("Detailed Product");
        product.Description.Should().Be("Full description");
        product.Price.Should().Be(299.99m);
        product.Currency.Should().Be("EUR");
        product.SKU.Should().Be("SKU-D01");
        product.StockQuantity.Should().Be(75);
        product.CategoryId.Should().Be(categoryResult.Id);
        product.CategoryName.Should().Be("Electronics");
    }
}

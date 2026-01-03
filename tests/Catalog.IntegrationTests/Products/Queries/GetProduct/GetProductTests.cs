using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Products.Queries.GetProduct;

namespace Catalog.IntegrationTests.Products.Queries.GetProduct;

using static Testing;

public class GetProductTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidProductId()
    {
        var query = new GetProductQuery(Guid.NewGuid());

        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldReturnProduct()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Smartphone",
            "A powerful smartphone",
            999.99m,
            "USD",
            "PHONE-001",
            50,
            categoryResult.Id));

        var query = new GetProductQuery(productResult.Id);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(productResult.Id);
        result.Name.Should().Be("Smartphone");
        result.Description.Should().Be("A powerful smartphone");
        result.Price.Should().Be(999.99m);
        result.Currency.Should().Be("USD");
        result.SKU.Should().Be("PHONE-001");
        result.StockQuantity.Should().Be(50);
        result.CategoryId.Should().Be(categoryResult.Id);
        result.CategoryName.Should().Be("Electronics");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task ShouldReturnProductWithZeroStock()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Out of Stock Item",
            "Currently unavailable",
            199.99m,
            "EUR",
            "OOS-001",
            0,
            categoryResult.Id));

        var query = new GetProductQuery(productResult.Id);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.StockQuantity.Should().Be(0);
    }

    [Test]
    public async Task ShouldReturnProductWithCategoryDetails()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Books",
            "All kinds of books"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Programming Book",
            "Learn to code",
            49.99m,
            "USD",
            "BOOK-001",
            100,
            categoryResult.Id));

        var query = new GetProductQuery(productResult.Id);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.CategoryId.Should().Be(categoryResult.Id);
        result.CategoryName.Should().Be("Books");
    }
}

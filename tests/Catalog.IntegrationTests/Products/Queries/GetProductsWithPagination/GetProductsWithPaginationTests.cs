using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Products.Queries.GetProductsWithPagination;

namespace Catalog.IntegrationTests.Products.Queries.GetProductsWithPagination;

using static Testing;

public class GetProductsWithPaginationTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyListWhenNoProducts()
    {
        var query = new GetProductsWithPaginationQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task ShouldReturnFirstPage()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        // Create 15 products
        for (int i = 1; i <= 15; i++)
        {
            await SendAsync(new CreateProductCommand(
                $"Product {i:D2}",
                $"Description {i}",
                99.99m + i,
                "USD",
                $"SKU-{i:D3}",
                10 + i,
                categoryResult.Id));
        }

        var query = new GetProductsWithPaginationQuery(PageNumber: 1, PageSize: 10);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(1);
        result.TotalPages.Should().Be(2);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Test]
    public async Task ShouldReturnSecondPage()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        // Create 15 products
        for (int i = 1; i <= 15; i++)
        {
            await SendAsync(new CreateProductCommand(
                $"Product {i:D2}",
                $"Description {i}",
                99.99m + i,
                "USD",
                $"SKU-{i:D3}",
                10 + i,
                categoryResult.Id));
        }

        var query = new GetProductsWithPaginationQuery(PageNumber: 2, PageSize: 10);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(2);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Test]
    public async Task ShouldFilterBySearchTermInName()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        await SendAsync(new CreateProductCommand("Smartphone Pro", "Phone", 999.99m, "USD", "SKU-001", 10, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Laptop Pro", "Computer", 1499.99m, "USD", "SKU-002", 5, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Smart Watch", "Watch", 299.99m, "USD", "SKU-003", 20, categoryResult.Id));

        var query = new GetProductsWithPaginationQuery(SearchTerm: "smart");

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2); // Smartphone Pro and Smart Watch
        result.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task ShouldFilterBySearchTermInDescription()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        await SendAsync(new CreateProductCommand("Product A", "Contains smartphone features", 999.99m, "USD", "SKU-001", 10, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Product B", "Basic features", 499.99m, "USD", "SKU-002", 15, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Product C", "Also has smartphone features", 799.99m, "USD", "SKU-003", 20, categoryResult.Id));

        var query = new GetProductsWithPaginationQuery(SearchTerm: "smartphone");

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task ShouldFilterBySearchTermInSKU()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        await SendAsync(new CreateProductCommand("Product A", "Desc A", 99.99m, "USD", "PHONE-001", 10, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Product B", "Desc B", 149.99m, "USD", "LAPTOP-001", 5, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Product C", "Desc C", 199.99m, "USD", "PHONE-002", 20, categoryResult.Id));

        var query = new GetProductsWithPaginationQuery(SearchTerm: "phone");

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task ShouldFilterByCategoryId()
    {
        var electronicsCategory = await SendAsync(new CreateCategoryCommand("Electronics", "Electronic devices"));
        var booksCategory = await SendAsync(new CreateCategoryCommand("Books", "All kinds of books"));

        await SendAsync(new CreateProductCommand("Smartphone", "Phone", 999.99m, "USD", "SKU-E01", 10, electronicsCategory.Id));
        await SendAsync(new CreateProductCommand("Laptop", "Computer", 1499.99m, "USD", "SKU-E02", 5, electronicsCategory.Id));
        await SendAsync(new CreateProductCommand("Programming Book", "Book", 49.99m, "USD", "SKU-B01", 50, booksCategory.Id));

        var query = new GetProductsWithPaginationQuery(CategoryId: electronicsCategory.Id);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(p => p.CategoryId == electronicsCategory.Id);
    }

    [Test]
    public async Task ShouldCombineSearchTermAndCategoryFilter()
    {
        var electronicsCategory = await SendAsync(new CreateCategoryCommand("Electronics", "Electronic devices"));
        var booksCategory = await SendAsync(new CreateCategoryCommand("Books", "All kinds of books"));

        await SendAsync(new CreateProductCommand("Smart Phone", "Phone", 999.99m, "USD", "SKU-E01", 10, electronicsCategory.Id));
        await SendAsync(new CreateProductCommand("Smart TV", "Television", 1499.99m, "USD", "SKU-E02", 5, electronicsCategory.Id));
        await SendAsync(new CreateProductCommand("Smart Investing Book", "Finance book", 29.99m, "USD", "SKU-B01", 100, booksCategory.Id));

        var query = new GetProductsWithPaginationQuery(SearchTerm: "smart", CategoryId: electronicsCategory.Id);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2); // Only Smart Phone and Smart TV from Electronics
        result.Items.Should().OnlyContain(p => p.CategoryId == electronicsCategory.Id);
    }

    [Test]
    public async Task ShouldReturnProductsOrderedByName()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        await SendAsync(new CreateProductCommand("Zebra Device", "Desc", 99.99m, "USD", "SKU-Z01", 10, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Apple Device", "Desc", 149.99m, "USD", "SKU-A01", 20, categoryResult.Id));
        await SendAsync(new CreateProductCommand("Mango Device", "Desc", 199.99m, "USD", "SKU-M01", 30, categoryResult.Id));

        var query = new GetProductsWithPaginationQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items[0].Name.Should().Be("Apple Device");
        result.Items[1].Name.Should().Be("Mango Device");
        result.Items[2].Name.Should().Be("Zebra Device");
    }

    [Test]
    public async Task ShouldHandleCustomPageSize()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        for (int i = 1; i <= 10; i++)
        {
            await SendAsync(new CreateProductCommand(
                $"Product {i}",
                $"Description {i}",
                99.99m + i,
                "USD",
                $"SKU-{i:D3}",
                10,
                categoryResult.Id));
        }

        var query = new GetProductsWithPaginationQuery(PageNumber: 1, PageSize: 3);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalPages.Should().Be(4); // 10 items / 3 per page = 4 pages
    }

    [Test]
    public async Task ShouldReturnProductsWithCategoryName()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        await SendAsync(new CreateProductCommand("Test Product", "Desc", 99.99m, "USD", "SKU-T01", 10, categoryResult.Id));

        var query = new GetProductsWithPaginationQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].CategoryName.Should().Be("Electronics");
    }
}

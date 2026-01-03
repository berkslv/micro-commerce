using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Categories.Queries.GetCategoriesWithPagination;

namespace Catalog.IntegrationTests.Categories.Queries.GetCategoriesWithPagination;

using static Testing;

public class GetCategoriesWithPaginationTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyListWhenNoCategories()
    {
        var query = new GetCategoriesWithPaginationQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task ShouldReturnFirstPage()
    {
        // Create 15 categories
        for (int i = 1; i <= 15; i++)
        {
            await SendAsync(new CreateCategoryCommand($"Category {i:D2}", $"Description {i}"));
        }

        var query = new GetCategoriesWithPaginationQuery(PageNumber: 1, PageSize: 10);

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
        // Create 15 categories
        for (int i = 1; i <= 15; i++)
        {
            await SendAsync(new CreateCategoryCommand($"Category {i:D2}", $"Description {i}"));
        }

        var query = new GetCategoriesWithPaginationQuery(PageNumber: 2, PageSize: 10);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(2);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Test]
    public async Task ShouldFilterBySearchTerm()
    {
        await SendAsync(new CreateCategoryCommand("Electronics", "Electronic devices"));
        await SendAsync(new CreateCategoryCommand("Books", "Reading materials"));
        await SendAsync(new CreateCategoryCommand("Electronic Accessories", "Phone cases etc"));

        var query = new GetCategoriesWithPaginationQuery(SearchTerm: "electronic");

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task ShouldFilterBySearchTermInDescription()
    {
        await SendAsync(new CreateCategoryCommand("Category A", "Contains electronics"));
        await SendAsync(new CreateCategoryCommand("Category B", "Contains books"));
        await SendAsync(new CreateCategoryCommand("Category C", "Contains electronics too"));

        var query = new GetCategoriesWithPaginationQuery(SearchTerm: "electronics");

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task ShouldReturnCategoriesOrderedByName()
    {
        await SendAsync(new CreateCategoryCommand("Zebra", "Z"));
        await SendAsync(new CreateCategoryCommand("Apple", "A"));
        await SendAsync(new CreateCategoryCommand("Mango", "M"));

        var query = new GetCategoriesWithPaginationQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items[0].Name.Should().Be("Apple");
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].Name.Should().Be("Zebra");
    }

    [Test]
    public async Task ShouldHandleCustomPageSize()
    {
        for (int i = 1; i <= 10; i++)
        {
            await SendAsync(new CreateCategoryCommand($"Category {i}", $"Description {i}"));
        }

        var query = new GetCategoriesWithPaginationQuery(PageNumber: 1, PageSize: 5);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalPages.Should().Be(2);
    }
}

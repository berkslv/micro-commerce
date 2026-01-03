using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Categories.Queries.GetCategories;

namespace Catalog.IntegrationTests.Categories.Queries.GetCategories;

using static Testing;

public class GetCategoriesTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyListWhenNoCategories()
    {
        var query = new GetCategoriesQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldReturnAllCategories()
    {
        await SendAsync(new CreateCategoryCommand("Electronics", "Electronic devices"));
        await SendAsync(new CreateCategoryCommand("Books", "All kinds of books"));
        await SendAsync(new CreateCategoryCommand("Clothing", "Fashion items"));

        var query = new GetCategoriesQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Test]
    public async Task ShouldReturnCategoriesOrderedByName()
    {
        await SendAsync(new CreateCategoryCommand("Zebra Category", "Z category"));
        await SendAsync(new CreateCategoryCommand("Apple Category", "A category"));
        await SendAsync(new CreateCategoryCommand("Mango Category", "M category"));

        var query = new GetCategoriesQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Apple Category");
        result[1].Name.Should().Be("Mango Category");
        result[2].Name.Should().Be("Zebra Category");
    }

    [Test]
    public async Task ShouldReturnCategoryDetails()
    {
        var createResult = await SendAsync(new CreateCategoryCommand(
            "Test Category",
            "Test Description"));

        var query = new GetCategoriesQuery();

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var category = result.First();
        category.Id.Should().Be(createResult.Id);
        category.Name.Should().Be("Test Category");
        category.Description.Should().Be("Test Description");
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }
}

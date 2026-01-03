using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Categories.Queries.GetCategory;

namespace Catalog.IntegrationTests.Categories.Queries.GetCategory;

using static Testing;

public class GetCategoryTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidCategoryId()
    {
        var query = new GetCategoryQuery(Guid.NewGuid());

        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldReturnCategory()
    {
        var createResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices and accessories"));

        var query = new GetCategoryQuery(createResult.Id);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Id.Should().Be(createResult.Id);
        result.Name.Should().Be("Electronics");
        result.Description.Should().Be("Electronic devices and accessories");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task ShouldReturnCategoryWithEmptyDescription()
    {
        var createResult = await SendAsync(new CreateCategoryCommand(
            "Books",
            ""));

        var query = new GetCategoryQuery(createResult.Id);

        var result = await SendAsync(query);

        result.Should().NotBeNull();
        result.Name.Should().Be("Books");
        result.Description.Should().BeEmpty();
    }
}

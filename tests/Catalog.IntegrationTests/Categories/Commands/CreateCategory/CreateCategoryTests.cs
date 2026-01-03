using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Categories.Commands.CreateCategory;

using static Testing;

public class CreateCategoryTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateCategoryCommand("", "");

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireValidName()
    {
        var command = new CreateCategoryCommand("A", "Description");

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldCreateCategory()
    {
        var command = new CreateCategoryCommand(
            "Electronics",
            "Electronic devices and accessories");

        var result = await SendAsync(command);

        var category = await FindAsync<Category>(result.Id);

        category.Should().NotBeNull();
        category!.Name.Should().Be(command.Name);
        category.Description.Should().Be(command.Description);
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task ShouldCreateCategoryWithEmptyDescription()
    {
        var command = new CreateCategoryCommand("Books", "");

        var result = await SendAsync(command);

        var category = await FindAsync<Category>(result.Id);

        category.Should().NotBeNull();
        category!.Name.Should().Be(command.Name);
        category.Description.Should().BeEmpty();
    }
}

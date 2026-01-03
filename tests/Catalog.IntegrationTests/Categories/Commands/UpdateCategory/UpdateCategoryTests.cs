using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Categories.Commands.UpdateCategory;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Categories.Commands.UpdateCategory;

using static Testing;

public class UpdateCategoryTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidCategoryId()
    {
        var command = new UpdateCategoryCommand(
            Guid.NewGuid(),
            "Updated Name",
            "Updated Description");

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var createResult = await SendAsync(new CreateCategoryCommand(
            "Original Category",
            "Original Description"));

        var command = new UpdateCategoryCommand(createResult.Id, "", "");

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldUpdateCategory()
    {
        var createResult = await SendAsync(new CreateCategoryCommand(
            "Original Category",
            "Original Description"));

        var command = new UpdateCategoryCommand(
            createResult.Id,
            "Updated Category",
            "Updated Description");

        await SendAsync(command);

        var category = await FindAsync<Category>(createResult.Id);

        category.Should().NotBeNull();
        category!.Name.Should().Be(command.Name);
        category.Description.Should().Be(command.Description);
        category.ModifiedAt.Should().NotBeNull();
        category.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task ShouldUpdateCategoryWithEmptyDescription()
    {
        var createResult = await SendAsync(new CreateCategoryCommand(
            "Original Category",
            "Original Description"));

        var command = new UpdateCategoryCommand(
            createResult.Id,
            "Updated Category",
            "");

        await SendAsync(command);

        var category = await FindAsync<Category>(createResult.Id);

        category.Should().NotBeNull();
        category!.Name.Should().Be(command.Name);
        category.Description.Should().BeEmpty();
    }
}

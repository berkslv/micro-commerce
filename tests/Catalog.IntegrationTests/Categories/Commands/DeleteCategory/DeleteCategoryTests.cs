using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Categories.Commands.DeleteCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Categories.Commands.DeleteCategory;

using static Testing;

public class DeleteCategoryTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidCategoryId()
    {
        var command = new DeleteCategoryCommand(Guid.NewGuid());

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldDeleteCategory()
    {
        var createResult = await SendAsync(new CreateCategoryCommand(
            "Category to Delete",
            "This category will be deleted"));

        await SendAsync(new DeleteCategoryCommand(createResult.Id));

        var category = await FindAsync<Category>(createResult.Id);

        category.Should().BeNull();
    }

    [Test]
    public async Task ShouldNotDeleteCategoryWithProducts()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Category with Products",
            "This category has products"));

        await SendAsync(new CreateProductCommand(
            "Test Product",
            "A test product",
            99.99m,
            "USD",
            "TEST-DEL-001",
            10,
            categoryResult.Id));

        var command = new DeleteCategoryCommand(categoryResult.Id);

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();

        // Verify category still exists
        var category = await FindAsync<Category>(categoryResult.Id);
        category.Should().NotBeNull();
    }
}

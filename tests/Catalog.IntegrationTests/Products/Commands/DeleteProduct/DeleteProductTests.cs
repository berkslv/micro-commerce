using BuildingBlocks.Common.Exceptions;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Products.Commands.DeleteProduct;
using Catalog.Domain.Entities;

namespace Catalog.IntegrationTests.Products.Commands.DeleteProduct;

using static Testing;

public class DeleteProductTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidProductId()
    {
        var command = new DeleteProductCommand(Guid.NewGuid());

        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldDeleteProduct()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Product to Delete",
            "This product will be deleted",
            99.99m,
            "USD",
            "DEL-001",
            10,
            categoryResult.Id));

        await SendAsync(new DeleteProductCommand(productResult.Id));

        var product = await FindAsync<Product>(productResult.Id);

        product.Should().BeNull();
    }

    [Test]
    public async Task ShouldDeleteProductAndKeepCategory()
    {
        var categoryResult = await SendAsync(new CreateCategoryCommand(
            "Electronics",
            "Electronic devices"));

        var productResult = await SendAsync(new CreateProductCommand(
            "Product to Delete",
            "This product will be deleted",
            99.99m,
            "USD",
            "DEL-002",
            10,
            categoryResult.Id));

        await SendAsync(new DeleteProductCommand(productResult.Id));

        var product = await FindAsync<Product>(productResult.Id);
        var category = await FindAsync<Category>(categoryResult.Id);

        product.Should().BeNull();
        category.Should().NotBeNull();
    }
}

using BuildingBlocks.Messaging.Events;
using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Products.Commands.UpdateProduct;
using Catalog.IntegrationTests.Infrastructure;
using MassTransit;
using MassTransit.Testing;

namespace Catalog.IntegrationTests.Messaging;

/// <summary>
/// Integration tests for product event publishing.
/// Verifies that domain events are properly published when products are created/updated/deleted.
/// </summary>
[Collection("CatalogApi")]
public class ProductEventsTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public ProductEventsTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithDatabase();
    }

    #region ProductCreatedEvent Tests

    [Fact]
    public async Task CreateProduct_PublishesProductCreatedEvent()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var harness = _factory.GetTestHarness();
        await harness.Start();

        var category = await CreateTestCategoryAsync();

        var command = new CreateProductCommand(
            Name: "Event Test Product",
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "EVENT-SKU-001",
            StockQuantity: 100,
            CategoryId: category.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);
        response.EnsureSuccessStatusCode();

        var createdProduct = await response.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Wait for event to be published
        await Task.Delay(500); // Give time for async event publishing

        // Assert
        var publishedEvents = harness.Published.Select<ProductCreatedEvent>().ToList();
        
        var productCreatedEvent = publishedEvents
            .FirstOrDefault(e => e.Context.Message.ProductId == createdProduct!.Id);

        productCreatedEvent.Should().NotBeNull();
        productCreatedEvent!.Context.Message.Name.Should().Be(command.Name);
        productCreatedEvent.Context.Message.Price.Should().Be(command.Price);
        productCreatedEvent.Context.Message.Currency.Should().Be(command.Currency);
        productCreatedEvent.Context.Message.StockQuantity.Should().Be(command.StockQuantity);
        productCreatedEvent.Context.Message.CategoryId.Should().Be(command.CategoryId);
        productCreatedEvent.Context.Message.IsAvailable.Should().BeTrue();

        await harness.Stop();
    }

    [Fact]
    public async Task CreateProduct_WithZeroStock_PublishesProductCreatedEventWithNotAvailable()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var harness = _factory.GetTestHarness();
        await harness.Start();

        var category = await CreateTestCategoryAsync();

        var command = new CreateProductCommand(
            Name: "Out of Stock Product",
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "OOS-SKU-001",
            StockQuantity: 0,
            CategoryId: category.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);
        response.EnsureSuccessStatusCode();

        var createdProduct = await response.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Wait for event to be published
        await Task.Delay(500);

        // Assert
        var publishedEvents = harness.Published.Select<ProductCreatedEvent>().ToList();
        
        var productCreatedEvent = publishedEvents
            .FirstOrDefault(e => e.Context.Message.ProductId == createdProduct!.Id);

        productCreatedEvent.Should().NotBeNull();
        productCreatedEvent!.Context.Message.StockQuantity.Should().Be(0);
        productCreatedEvent.Context.Message.IsAvailable.Should().BeFalse();

        await harness.Stop();
    }

    #endregion

    #region ProductUpdatedEvent Tests

    [Fact]
    public async Task UpdateProduct_PublishesProductUpdatedEvent()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var harness = _factory.GetTestHarness();
        await harness.Start();

        var existingProduct = await CreateTestProductAsync();

        var updateCommand = new UpdateProductCommand(
            Id: existingProduct.Id,
            Name: "Updated Event Product",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "EUR",
            StockQuantity: 200,
            CategoryId: existingProduct.CategoryId);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{existingProduct.Id}", updateCommand);
        response.EnsureSuccessStatusCode();

        // Wait for event to be published
        await Task.Delay(500);

        // Assert
        var publishedEvents = harness.Published.Select<ProductUpdatedEvent>().ToList();
        
        var productUpdatedEvent = publishedEvents
            .FirstOrDefault(e => e.Context.Message.ProductId == existingProduct.Id);

        productUpdatedEvent.Should().NotBeNull();
        productUpdatedEvent!.Context.Message.Name.Should().Be(updateCommand.Name);
        productUpdatedEvent.Context.Message.Price.Should().Be(updateCommand.Price);
        productUpdatedEvent.Context.Message.Currency.Should().Be(updateCommand.Currency);

        await harness.Stop();
    }

    [Fact]
    public async Task UpdateProduct_WithPriceChange_PublishesProductUpdatedEventWithCorrectData()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var harness = _factory.GetTestHarness();
        await harness.Start();

        var existingProduct = await CreateTestProductAsync();

        var updateCommand = new UpdateProductCommand(
            Id: existingProduct.Id,
            Name: "Price Changed Product",
            Description: "Updated Description",
            Price: 199.99m, // Price change
            Currency: "EUR", // Currency change
            StockQuantity: existingProduct.StockQuantity, // Stock not changed through Update
            CategoryId: existingProduct.CategoryId);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{existingProduct.Id}", updateCommand);
        response.EnsureSuccessStatusCode();

        // Wait for event to be published
        await Task.Delay(500);

        // Assert
        var publishedEvents = harness.Published.Select<ProductUpdatedEvent>().ToList();
        
        var productUpdatedEvent = publishedEvents
            .FirstOrDefault(e => e.Context.Message.ProductId == existingProduct.Id);

        productUpdatedEvent.Should().NotBeNull();
        productUpdatedEvent!.Context.Message.Name.Should().Be("Price Changed Product");
        productUpdatedEvent.Context.Message.Price.Should().Be(199.99m);
        productUpdatedEvent.Context.Message.Currency.Should().Be("EUR");

        await harness.Stop();
    }

    #endregion

    #region ProductDeletedEvent Tests

    // Note: Current implementation doesn't raise ProductDeletedEvent from domain
    // The delete operation just removes the entity without raising a domain event
    // This test is skipped as it represents a potential improvement to the domain model

    // [Fact]
    // public async Task DeleteProduct_PublishesProductDeletedEvent()
    // {
    //     // When implemented, this test should verify ProductDeletedEvent is published
    // }

    #endregion

    #region Event Sequence Tests

    [Fact]
    public async Task ProductLifecycle_PublishesCreatedAndUpdatedEvents()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var harness = _factory.GetTestHarness();
        await harness.Start();

        var category = await CreateTestCategoryAsync();

        // Create product
        var createCommand = new CreateProductCommand(
            Name: "Lifecycle Product",
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "LIFECYCLE-SKU-001",
            StockQuantity: 100,
            CategoryId: category.Id);

        var createResponse = await _client.PostAsJsonAsync("/api/products", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Update product
        var updateCommand = new UpdateProductCommand(
            Id: createdProduct!.Id,
            Name: "Updated Lifecycle Product",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "EUR",
            StockQuantity: 200,
            CategoryId: category.Id);

        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{createdProduct.Id}", updateCommand);
        updateResponse.EnsureSuccessStatusCode();

        // Wait for all events to be published
        await Task.Delay(500);

        // Assert - verify created and updated events were published
        var createdEvents = harness.Published.Select<ProductCreatedEvent>().ToList();
        var updatedEvents = harness.Published.Select<ProductUpdatedEvent>().ToList();

        createdEvents.Should().ContainSingle(e => e.Context.Message.ProductId == createdProduct.Id);
        updatedEvents.Should().ContainSingle(e => e.Context.Message.ProductId == createdProduct.Id);

        // Verify event data
        var createdEvent = createdEvents.First(e => e.Context.Message.ProductId == createdProduct.Id);
        createdEvent.Context.Message.Name.Should().Be("Lifecycle Product");
        createdEvent.Context.Message.Price.Should().Be(99.99m);

        var updatedEvent = updatedEvents.First(e => e.Context.Message.ProductId == createdProduct.Id);
        updatedEvent.Context.Message.Name.Should().Be("Updated Lifecycle Product");
        updatedEvent.Context.Message.Price.Should().Be(149.99m);

        await harness.Stop();
    }

    #endregion

    #region Helper Methods

    private async Task<CreateCategoryResponse> CreateTestCategoryAsync(
        string name = "Test Category",
        string description = "Test Description")
    {
        var command = new CreateCategoryCommand(name, description);
        var response = await _client.PostAsJsonAsync("/api/categories", command);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateCategoryResponse>())!;
    }

    private async Task<CreateProductResponse> CreateTestProductAsync(
        string name = "Test Product",
        string sku = "TEST-SKU")
    {
        var category = await CreateTestCategoryAsync($"Category for {name}", "Category Description");

        var command = new CreateProductCommand(
            Name: name,
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: sku,
            StockQuantity: 100,
            CategoryId: category.Id);

        var response = await _client.PostAsJsonAsync("/api/products", command);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateProductResponse>())!;
    }

    #endregion
}

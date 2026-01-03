using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Products.Commands.UpdateProduct;
using Catalog.Application.Features.Products.Queries.GetProduct;
using Catalog.Application.Features.Products.Queries.GetProducts;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Api;

/// <summary>
/// Integration tests for ProductsController.
/// </summary>
[Collection("CatalogApi")]
public class ProductsControllerTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public ProductsControllerTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithDatabase();
    }

    #region Create Product Tests

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreatedProduct()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var category = await CreateTestCategoryAsync();

        var command = new CreateProductCommand(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "TEST-SKU-001",
            StockQuantity: 100,
            CategoryId: category.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.Price.Should().Be(command.Price);
        result.Currency.Should().Be(command.Currency);
        result.SKU.Should().Be(command.SKU);
        result.StockQuantity.Should().Be(command.StockQuantity);
        result.CategoryId.Should().Be(command.CategoryId);

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(result.Id.ToString());
    }

    [Fact]
    public async Task CreateProduct_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var category = await CreateTestCategoryAsync();

        var command = new CreateProductCommand(
            Name: "", // Invalid - empty name
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "TEST-SKU-002",
            StockQuantity: 100,
            CategoryId: category.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithNegativePrice_ReturnsBadRequest()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var category = await CreateTestCategoryAsync();

        var command = new CreateProductCommand(
            Name: "Test Product",
            Description: "Test Description",
            Price: -10.00m, // Invalid - negative price
            Currency: "USD",
            SKU: "TEST-SKU-003",
            StockQuantity: 100,
            CategoryId: category.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithNonExistentCategory_ReturnsNotFound()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var command = new CreateProductCommand(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "TEST-SKU-004",
            StockQuantity: 100,
            CategoryId: Guid.NewGuid()); // Non-existent category

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Product Tests

    [Fact]
    public async Task GetProduct_WithExistingId_ReturnsProduct()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var createdProduct = await CreateTestProductAsync();

        // Act
        var response = await _client.GetAsync($"/api/products/{createdProduct.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetProductResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdProduct.Id);
        result.Name.Should().Be(createdProduct.Name);
        result.SKU.Should().Be(createdProduct.SKU);
    }

    [Fact]
    public async Task GetProduct_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/products/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await CreateTestProductAsync("Product 1", "SKU-001");
        await CreateTestProductAsync("Product 2", "SKU-002");
        await CreateTestProductAsync("Product 3", "SKU-003");

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GetProductsResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetProducts_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GetProductsResponse>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    #endregion

    #region Update Product Tests

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsUpdatedProduct()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var existingProduct = await CreateTestProductAsync();

        var command = new UpdateProductCommand(
            Id: existingProduct.Id,
            Name: "Updated Product Name",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "USD",
            StockQuantity: 200,
            CategoryId: existingProduct.CategoryId);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{existingProduct.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UpdateProductResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingProduct.Id);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.Price.Should().Be(command.Price);
        result.StockQuantity.Should().Be(command.StockQuantity);
    }

    [Fact]
    public async Task UpdateProduct_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var existingProduct = await CreateTestProductAsync();

        var command = new UpdateProductCommand(
            Id: Guid.NewGuid(), // Different ID
            Name: "Updated Product Name",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "USD",
            StockQuantity: 200,
            CategoryId: existingProduct.CategoryId);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{existingProduct.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var category = await CreateTestCategoryAsync();
        var nonExistingId = Guid.NewGuid();

        var command = new UpdateProductCommand(
            Id: nonExistingId,
            Name: "Updated Product Name",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "USD",
            StockQuantity: 200,
            CategoryId: category.Id);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{nonExistingId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Product Tests

    [Fact]
    public async Task DeleteProduct_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var existingProduct = await CreateTestProductAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{existingProduct.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify product is deleted
        var getResponse = await _client.GetAsync($"/api/products/{existingProduct.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Database Integration Tests

    [Fact]
    public async Task ProductCrud_VerifiesDatabasePersistence()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var category = await CreateTestCategoryAsync();

        // Create product
        var createCommand = new CreateProductCommand(
            Name: "Persistent Product",
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "PERSIST-SKU-001",
            StockQuantity: 100,
            CategoryId: category.Id);

        var createResponse = await _client.PostAsJsonAsync("/api/products", createCommand);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();

        // Update product
        var updateCommand = new UpdateProductCommand(
            Id: createdProduct!.Id,
            Name: "Updated Persistent Product",
            Description: "Updated Description",
            Price: 199.99m,
            Currency: "EUR",
            StockQuantity: 50,
            CategoryId: category.Id);

        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{createdProduct.Id}", updateCommand);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify update persisted
        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedProduct = await getResponse.Content.ReadFromJsonAsync<GetProductResponse>();
        retrievedProduct!.Name.Should().Be("Updated Persistent Product");
        retrievedProduct.Price.Should().Be(199.99m);

        // Delete product
        var deleteResponse = await _client.DeleteAsync($"/api/products/{createdProduct.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion persisted
        var verifyDeleteResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        verifyDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

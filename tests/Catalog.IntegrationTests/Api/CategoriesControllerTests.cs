using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Categories.Commands.UpdateCategory;
using Catalog.Application.Features.Categories.Queries.GetCategory;
using Catalog.Application.Features.Categories.Queries.GetCategories;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Api;

/// <summary>
/// Integration tests for CategoriesController.
/// </summary>
[Collection("CatalogApi")]
public class CategoriesControllerTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public CategoriesControllerTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithDatabase();
    }

    #region Create Category Tests

    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreatedCategory()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var command = new CreateCategoryCommand(
            Name: "Electronics",
            Description: "Electronic devices and accessories");

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateCategoryResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(result.Id.ToString());
    }

    [Fact]
    public async Task CreateCategory_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var command = new CreateCategoryCommand(
            Name: "", // Invalid - empty name
            Description: "Test Description");

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_WithNameExceedingMaxLength_ReturnsBadRequest()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var command = new CreateCategoryCommand(
            Name: new string('A', 201), // Exceeds max length (typically 200)
            Description: "Test Description");

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Category Tests

    [Fact]
    public async Task GetCategory_WithExistingId_ReturnsCategory()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var createdCategory = await CreateTestCategoryAsync();

        // Act
        var response = await _client.GetAsync($"/api/categories/{createdCategory.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetCategoryResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdCategory.Id);
        result.Name.Should().Be(createdCategory.Name);
        result.Description.Should().Be(createdCategory.Description);
    }

    [Fact]
    public async Task GetCategory_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/categories/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCategories_ReturnsAllCategories()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        await CreateTestCategoryAsync("Electronics", "Electronic devices");
        await CreateTestCategoryAsync("Clothing", "Clothing items");
        await CreateTestCategoryAsync("Books", "Books and publications");

        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GetCategoriesResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result.Should().Contain(c => c.Name == "Electronics");
        result.Should().Contain(c => c.Name == "Clothing");
        result.Should().Contain(c => c.Name == "Books");
    }

    [Fact]
    public async Task GetCategories_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GetCategoriesResponse>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    #endregion

    #region Update Category Tests

    [Fact]
    public async Task UpdateCategory_WithValidData_ReturnsUpdatedCategory()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var existingCategory = await CreateTestCategoryAsync();

        var command = new UpdateCategoryCommand(
            Id: existingCategory.Id,
            Name: "Updated Electronics",
            Description: "Updated description for electronics");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/categories/{existingCategory.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UpdateCategoryResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingCategory.Id);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
    }

    [Fact]
    public async Task UpdateCategory_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var existingCategory = await CreateTestCategoryAsync();

        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(), // Different ID
            Name: "Updated Name",
            Description: "Updated description");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/categories/{existingCategory.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var nonExistingId = Guid.NewGuid();

        var command = new UpdateCategoryCommand(
            Id: nonExistingId,
            Name: "Updated Name",
            Description: "Updated description");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/categories/{nonExistingId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var existingCategory = await CreateTestCategoryAsync();

        var command = new UpdateCategoryCommand(
            Id: existingCategory.Id,
            Name: "", // Invalid - empty name
            Description: "Updated description");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/categories/{existingCategory.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Delete Category Tests

    [Fact]
    public async Task DeleteCategory_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var existingCategory = await CreateTestCategoryAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/categories/{existingCategory.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify category is deleted
        var getResponse = await _client.GetAsync($"/api/categories/{existingCategory.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/categories/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Database Integration Tests

    [Fact]
    public async Task CategoryCrud_VerifiesDatabasePersistence()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Create category
        var createCommand = new CreateCategoryCommand(
            Name: "Persistent Category",
            Description: "Test Description");

        var createResponse = await _client.PostAsJsonAsync("/api/categories", createCommand);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<CreateCategoryResponse>();

        // Update category
        var updateCommand = new UpdateCategoryCommand(
            Id: createdCategory!.Id,
            Name: "Updated Persistent Category",
            Description: "Updated Description");

        var updateResponse = await _client.PutAsJsonAsync($"/api/categories/{createdCategory.Id}", updateCommand);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify update persisted
        var getResponse = await _client.GetAsync($"/api/categories/{createdCategory.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedCategory = await getResponse.Content.ReadFromJsonAsync<GetCategoryResponse>();
        retrievedCategory!.Name.Should().Be("Updated Persistent Category");

        // Delete category
        var deleteResponse = await _client.DeleteAsync($"/api/categories/{createdCategory.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion persisted
        var verifyDeleteResponse = await _client.GetAsync($"/api/categories/{createdCategory.Id}");
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

    #endregion
}

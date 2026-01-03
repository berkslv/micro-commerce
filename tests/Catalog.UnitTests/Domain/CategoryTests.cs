using BuildingBlocks.Common.Exceptions;
using Catalog.Domain.Entities;

namespace Catalog.UnitTests.Domain;

public class CategoryTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateCategory()
    {
        // Arrange
        var name = "Electronics";
        var description = "Electronic devices and accessories";

        // Act
        var category = Category.Create(name, description);

        // Assert
        category.Should().NotBeNull();
        category.Id.Should().NotBeEmpty();
        category.Name.Should().Be(name);
        category.Description.Should().Be(description);
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullDescription_ShouldCreateCategoryWithEmptyDescription()
    {
        // Arrange
        var name = "Electronics";

        // Act
        var category = Category.Create(name, null!);

        // Assert
        category.Should().NotBeNull();
        category.Description.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        var act = () => Category.Create(invalidName!, "Description");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Category name is required");
    }

    [Fact]
    public void Create_ShouldInitializeProductsAsEmptyCollection()
    {
        // Arrange & Act
        var category = Category.Create("Electronics", "Electronic devices");

        // Assert
        category.Products.Should().NotBeNull();
        category.Products.Should().BeEmpty();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateCategory()
    {
        // Arrange
        var category = CreateTestCategory();
        var newName = "Updated Electronics";
        var newDescription = "Updated description for electronics";

        // Act
        category.Update(newName, newDescription);

        // Assert
        category.Name.Should().Be(newName);
        category.Description.Should().Be(newDescription);
        category.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_WithNullDescription_ShouldSetEmptyDescription()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.Update("Updated Name", null!);

        // Assert
        category.Description.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        var act = () => category.Update(invalidName!, "New Description");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Category name is required");
    }

    [Fact]
    public void Update_ShouldNotChangeId()
    {
        // Arrange
        var category = CreateTestCategory();
        var originalId = category.Id;

        // Act
        category.Update("New Name", "New Description");

        // Assert
        category.Id.Should().Be(originalId);
    }

    [Fact]
    public void Update_ShouldNotChangeCreatedAt()
    {
        // Arrange
        var category = CreateTestCategory();
        var originalCreatedAt = category.CreatedAt;

        // Act
        category.Update("New Name", "New Description");

        // Assert
        category.CreatedAt.Should().Be(originalCreatedAt);
    }

    #endregion

    #region Helper Methods

    private static Category CreateTestCategory()
    {
        return Category.Create("Test Category", "Test Description");
    }

    #endregion
}

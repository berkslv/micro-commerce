using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;

namespace Catalog.UnitTests.Domain;

public class ProductTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateProduct()
    {
        // Arrange
        var name = ProductName.Create("Test Product");
        var description = "Test Description";
        var price = Money.Create(99.99m, "USD");
        var stockQuantity = 100;
        var sku = Sku.Create("TEST-SKU-001");
        var categoryId = Guid.NewGuid();

        // Act
        var product = Product.Create(name, description, price, stockQuantity, sku, categoryId);

        // Assert
        product.Should().NotBeNull();
        product.Id.Should().NotBeEmpty();
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.Price.Should().Be(price);
        product.StockQuantity.Should().Be(stockQuantity);
        product.Sku.Should().Be(sku);
        product.CategoryId.Should().Be(categoryId);
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullDescription_ShouldCreateProductWithEmptyDescription()
    {
        // Arrange
        var name = ProductName.Create("Test Product");
        var price = Money.Create(99.99m, "USD");
        var sku = Sku.Create("TEST-SKU-001");
        var categoryId = Guid.NewGuid();

        // Act
        var product = Product.Create(name, null!, price, 100, sku, categoryId);

        // Assert
        product.Description.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithNegativeStockQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var name = ProductName.Create("Test Product");
        var price = Money.Create(99.99m, "USD");
        var sku = Sku.Create("TEST-SKU-001");
        var categoryId = Guid.NewGuid();

        // Act
        var act = () => Product.Create(name, "Description", price, -1, sku, categoryId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Stock quantity cannot be negative");
    }

    [Fact]
    public void Create_ShouldAddProductCreatedDomainEvent()
    {
        // Arrange
        var name = ProductName.Create("Test Product");
        var price = Money.Create(99.99m, "USD");
        var sku = Sku.Create("TEST-SKU-001");
        var categoryId = Guid.NewGuid();

        // Act
        var product = Product.Create(name, "Description", price, 100, sku, categoryId);

        // Assert
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedEvent>();

        var domainEvent = (ProductCreatedEvent)product.DomainEvents.First();
        domainEvent.ProductId.Should().Be(product.Id);
        domainEvent.Name.Should().Be(name.Value);
        domainEvent.Price.Should().Be(price.Amount);
        domainEvent.Currency.Should().Be(price.Currency);
        domainEvent.StockQuantity.Should().Be(100);
        domainEvent.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Create_WithZeroStock_ShouldSetIsAvailableFalseInEvent()
    {
        // Arrange
        var name = ProductName.Create("Test Product");
        var price = Money.Create(99.99m, "USD");
        var sku = Sku.Create("TEST-SKU-001");
        var categoryId = Guid.NewGuid();

        // Act
        var product = Product.Create(name, "Description", price, 0, sku, categoryId);

        // Assert
        var domainEvent = (ProductCreatedEvent)product.DomainEvents.First();
        domainEvent.IsAvailable.Should().BeFalse();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateProduct()
    {
        // Arrange
        var product = CreateTestProduct();
        var newName = ProductName.Create("Updated Product");
        var newDescription = "Updated Description";
        var newPrice = Money.Create(149.99m, "EUR");

        // Act
        product.Update(newName, newDescription, newPrice);

        // Assert
        product.Name.Should().Be(newName);
        product.Description.Should().Be(newDescription);
        product.Price.Should().Be(newPrice);
        product.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_ShouldAddProductUpdatedDomainEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();
        var newName = ProductName.Create("Updated Product");
        var newPrice = Money.Create(149.99m, "EUR");

        // Act
        product.Update(newName, "Updated Description", newPrice);

        // Assert
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUpdatedEvent>();

        var domainEvent = (ProductUpdatedEvent)product.DomainEvents.First();
        domainEvent.ProductId.Should().Be(product.Id);
        domainEvent.Name.Should().Be(newName.Value);
        domainEvent.Price.Should().Be(newPrice.Amount);
    }

    #endregion

    #region UpdateStock Tests

    [Fact]
    public void UpdateStock_WithValidQuantity_ShouldUpdateStockQuantity()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 50);
        product.ClearDomainEvents();

        // Act
        product.UpdateStock(75);

        // Assert
        product.StockQuantity.Should().Be(75);
        product.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateStock_WithNegativeQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 50);

        // Act
        var act = () => product.UpdateStock(-1);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Stock quantity cannot be negative");
    }

    [Fact]
    public void UpdateStock_ShouldAddProductUpdatedDomainEvent()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 50);
        product.ClearDomainEvents();

        // Act
        product.UpdateStock(100);

        // Assert
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUpdatedEvent>();
    }

    #endregion

    #region ReserveStock Tests

    [Fact]
    public void ReserveStock_WithSufficientStock_ShouldReserveAndReturnTrue()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 100);

        // Act
        var result = product.ReserveStock(30);

        // Assert
        result.Should().BeTrue();
        product.StockQuantity.Should().Be(70);
        product.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReserveStock_WithExactStock_ShouldReserveAndReturnTrue()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 50);

        // Act
        var result = product.ReserveStock(50);

        // Assert
        result.Should().BeTrue();
        product.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_ShouldReturnFalse()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 10);

        // Act
        var result = product.ReserveStock(20);

        // Assert
        result.Should().BeFalse();
        product.StockQuantity.Should().Be(10); // Unchanged
    }

    [Fact]
    public void ReserveStock_WithZeroQuantity_ShouldReturnFalse()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 100);

        // Act
        var result = product.ReserveStock(0);

        // Assert
        result.Should().BeFalse();
        product.StockQuantity.Should().Be(100); // Unchanged
    }

    [Fact]
    public void ReserveStock_WithNegativeQuantity_ShouldReturnFalse()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 100);

        // Act
        var result = product.ReserveStock(-5);

        // Assert
        result.Should().BeFalse();
        product.StockQuantity.Should().Be(100); // Unchanged
    }

    #endregion

    #region RestoreStock Tests

    [Fact]
    public void RestoreStock_WithValidQuantity_ShouldIncreaseStock()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 50);

        // Act
        product.RestoreStock(25);

        // Assert
        product.StockQuantity.Should().Be(75);
        product.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RestoreStock_WithZeroQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 50);

        // Act
        var act = () => product.RestoreStock(0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Quantity must be greater than zero");
    }

    [Fact]
    public void RestoreStock_WithNegativeQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 50);

        // Act
        var act = () => product.RestoreStock(-10);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Quantity must be greater than zero");
    }

    #endregion

    #region ReleaseStock Tests

    [Fact]
    public void ReleaseStock_WithValidQuantity_ShouldIncreaseStock()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 30);

        // Act
        product.ReleaseStock(20);

        // Assert
        product.StockQuantity.Should().Be(50);
    }

    [Fact]
    public void ReleaseStock_WithZeroQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 30);

        // Act
        var act = () => product.ReleaseStock(0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Release quantity must be positive");
    }

    [Fact]
    public void ReleaseStock_WithNegativeQuantity_ShouldThrowDomainException()
    {
        // Arrange
        var product = CreateTestProduct(stockQuantity: 30);

        // Act
        var act = () => product.ReleaseStock(-5);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Release quantity must be positive");
    }

    #endregion

    #region ClearDomainEvents Tests

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var product = CreateTestProduct();
        product.DomainEvents.Should().NotBeEmpty();

        // Act
        product.ClearDomainEvents();

        // Assert
        product.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static Product CreateTestProduct(int stockQuantity = 100)
    {
        return Product.Create(
            ProductName.Create("Test Product"),
            "Test Description",
            Money.Create(99.99m, "USD"),
            stockQuantity,
            Sku.Create("TEST-SKU-001"),
            Guid.NewGuid());
    }

    #endregion
}

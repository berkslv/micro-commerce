using BuildingBlocks.Common.Exceptions;
using Order.Domain.Entities;
using Order.Domain.ValueObjects;

namespace Order.UnitTests.Domain;

public class OrderItemTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateOrderItem()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var productName = "Test Product";
        var unitPrice = Money.Create(25.99m, "USD");
        var quantity = 3;

        // Act
        var orderItem = OrderItem.Create(orderId, productId, productName, unitPrice, quantity);

        // Assert
        orderItem.Should().NotBeNull();
        orderItem.Id.Should().NotBeEmpty();
        orderItem.OrderId.Should().Be(orderId);
        orderItem.ProductId.Should().Be(productId);
        orderItem.ProductName.Should().Be(productName);
        orderItem.UnitPrice.Should().Be(unitPrice);
        orderItem.Quantity.Should().Be(quantity);
    }

    [Fact]
    public void Create_ShouldCalculateTotalPriceCorrectly()
    {
        // Arrange
        var unitPrice = Money.Create(10.00m, "USD");
        var quantity = 5;

        // Act
        var orderItem = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Product", unitPrice, quantity);

        // Assert
        orderItem.TotalPrice.Amount.Should().Be(50.00m);
        orderItem.TotalPrice.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithEmptyProductId_ShouldThrowDomainException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var unitPrice = Money.Create(10.00m, "USD");

        // Act
        var act = () => OrderItem.Create(orderId, Guid.Empty, "Product", unitPrice, 1);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Product ID is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidProductName_ShouldThrowDomainException(string? invalidName)
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var unitPrice = Money.Create(10.00m, "USD");

        // Act
        var act = () => OrderItem.Create(orderId, productId, invalidName!, unitPrice, 1);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Product name is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidQuantity_ShouldThrowDomainException(int invalidQuantity)
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var unitPrice = Money.Create(10.00m, "USD");

        // Act
        var act = () => OrderItem.Create(orderId, productId, "Product", unitPrice, invalidQuantity);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Quantity must be greater than zero.");
    }

    [Fact]
    public void Create_WithSingleQuantity_ShouldHaveMatchingTotalPrice()
    {
        // Arrange
        var unitPrice = Money.Create(99.99m, "USD");

        // Act
        var orderItem = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Product", unitPrice, 1);

        // Assert
        orderItem.TotalPrice.Amount.Should().Be(unitPrice.Amount);
    }

    #endregion

    #region UpdateQuantity Tests

    [Fact]
    public void UpdateQuantity_WithValidQuantity_ShouldUpdateQuantity()
    {
        // Arrange
        var orderItem = CreateTestOrderItem(quantity: 2);

        // Act
        orderItem.UpdateQuantity(5);

        // Assert
        orderItem.Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateQuantity_ShouldUpdateTotalPrice()
    {
        // Arrange
        var unitPrice = Money.Create(10.00m, "USD");
        var orderItem = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Product", unitPrice, 2);

        // Act
        orderItem.UpdateQuantity(4);

        // Assert
        orderItem.TotalPrice.Amount.Should().Be(40.00m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateQuantity_WithInvalidQuantity_ShouldThrowDomainException(int invalidQuantity)
    {
        // Arrange
        var orderItem = CreateTestOrderItem();

        // Act
        var act = () => orderItem.UpdateQuantity(invalidQuantity);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Quantity must be greater than zero.");
    }

    #endregion

    #region TotalPrice Tests

    [Theory]
    [InlineData(10.00, 1, 10.00)]
    [InlineData(10.00, 2, 20.00)]
    [InlineData(15.50, 3, 46.50)]
    [InlineData(99.99, 10, 999.90)]
    public void TotalPrice_ShouldCalculateCorrectly(decimal unitPriceAmount, int quantity, decimal expectedTotal)
    {
        // Arrange
        var unitPrice = Money.Create(unitPriceAmount, "USD");
        var orderItem = OrderItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Product", unitPrice, quantity);

        // Assert
        orderItem.TotalPrice.Amount.Should().Be(expectedTotal);
    }

    #endregion

    #region Helper Methods

    private static OrderItem CreateTestOrderItem(int quantity = 1)
    {
        return OrderItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Product",
            Money.Create(25.99m, "USD"),
            quantity);
    }

    #endregion
}

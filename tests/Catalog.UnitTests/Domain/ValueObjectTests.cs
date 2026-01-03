using BuildingBlocks.Common.Exceptions;
using Catalog.Domain.ValueObjects;

namespace Catalog.UnitTests.Domain;

public class ValueObjectTests
{
    #region ProductName Tests

    [Fact]
    public void ProductName_Create_WithValidValue_ShouldCreateProductName()
    {
        // Arrange
        var value = "Test Product";

        // Act
        var productName = ProductName.Create(value);

        // Assert
        productName.Should().NotBeNull();
        productName.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductName_Create_WithInvalidValue_ShouldThrowDomainException(string? invalidValue)
    {
        // Act
        var act = () => ProductName.Create(invalidValue!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Product name is required");
    }

    [Fact]
    public void ProductName_Create_WithExceedingLength_ShouldThrowDomainException()
    {
        // Arrange
        var longValue = new string('a', 201);

        // Act
        var act = () => ProductName.Create(longValue);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Product name cannot exceed 200 characters");
    }

    [Fact]
    public void ProductName_Create_WithMaxLength_ShouldSucceed()
    {
        // Arrange
        var maxLengthValue = new string('a', 200);

        // Act
        var productName = ProductName.Create(maxLengthValue);

        // Assert
        productName.Value.Should().HaveLength(200);
    }

    [Fact]
    public void ProductName_Equality_ShouldCompareByValue()
    {
        // Arrange
        var name1 = ProductName.Create("Product A");
        var name2 = ProductName.Create("Product A");
        var name3 = ProductName.Create("Product B");

        // Assert
        name1.Should().Be(name2);
        name1.Should().NotBe(name3);
    }

    [Fact]
    public void ProductName_ImplicitConversion_ShouldReturnValue()
    {
        // Arrange
        var productName = ProductName.Create("Test Product");

        // Act
        string value = productName;

        // Assert
        value.Should().Be("Test Product");
    }

    #endregion

    #region Money Tests

    [Fact]
    public void Money_Create_WithValidAmount_ShouldCreateMoney()
    {
        // Arrange
        var amount = 99.99m;
        var currency = "USD";

        // Act
        var money = Money.Create(amount, currency);

        // Assert
        money.Should().NotBeNull();
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Create_WithDefaultCurrency_ShouldUseUSD()
    {
        // Act
        var money = Money.Create(50.00m);

        // Assert
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Create_WithLowercaseCurrency_ShouldConvertToUppercase()
    {
        // Act
        var money = Money.Create(100.00m, "eur");

        // Assert
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Money_Create_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Act
        var act = () => Money.Create(-10.00m, "USD");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Price cannot be negative");
    }

    [Fact]
    public void Money_Create_WithZeroAmount_ShouldSucceed()
    {
        // Act
        var money = Money.Create(0m, "USD");

        // Assert
        money.Amount.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Money_Create_WithInvalidCurrency_ShouldThrowDomainException(string? invalidCurrency)
    {
        // Act
        var act = () => Money.Create(100.00m, invalidCurrency!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Currency is required");
    }

    [Fact]
    public void Money_Addition_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = Money.Create(50.00m, "USD");
        var money2 = Money.Create(30.00m, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(80.00m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Addition_WithDifferentCurrencies_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = Money.Create(50.00m, "USD");
        var money2 = Money.Create(30.00m, "EUR");

        // Act
        var act = () => money1 + money2;

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot add money with different currencies");
    }

    [Fact]
    public void Money_Multiplication_ShouldReturnProduct()
    {
        // Arrange
        var money = Money.Create(25.00m, "USD");

        // Act
        var result = money * 3;

        // Assert
        result.Amount.Should().Be(75.00m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Equality_ShouldCompareByAmountAndCurrency()
    {
        // Arrange
        var money1 = Money.Create(100.00m, "USD");
        var money2 = Money.Create(100.00m, "USD");
        var money3 = Money.Create(100.00m, "EUR");
        var money4 = Money.Create(50.00m, "USD");

        // Assert
        money1.Should().Be(money2);
        money1.Should().NotBe(money3);
        money1.Should().NotBe(money4);
    }

    [Fact]
    public void Money_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = Money.Create(99.99m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("99.99 USD");
    }

    #endregion

    #region SKU Tests

    [Fact]
    public void Sku_Create_WithValidValue_ShouldCreateSku()
    {
        // Arrange
        var value = "SKU-001";

        // Act
        var sku = Sku.Create(value);

        // Assert
        sku.Should().NotBeNull();
        sku.Value.Should().Be("SKU-001");
    }

    [Fact]
    public void Sku_Create_WithLowercaseValue_ShouldConvertToUppercase()
    {
        // Act
        var sku = Sku.Create("sku-abc-123");

        // Assert
        sku.Value.Should().Be("SKU-ABC-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sku_Create_WithInvalidValue_ShouldThrowDomainException(string? invalidValue)
    {
        // Act
        var act = () => Sku.Create(invalidValue!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("SKU is required");
    }

    [Fact]
    public void Sku_Create_WithExceedingLength_ShouldThrowDomainException()
    {
        // Arrange
        var longValue = new string('a', 51);

        // Act
        var act = () => Sku.Create(longValue);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("SKU cannot exceed 50 characters");
    }

    [Fact]
    public void Sku_Create_WithMaxLength_ShouldSucceed()
    {
        // Arrange
        var maxLengthValue = new string('a', 50);

        // Act
        var sku = Sku.Create(maxLengthValue);

        // Assert
        sku.Value.Should().HaveLength(50);
    }

    [Fact]
    public void Sku_Equality_ShouldCompareByValue()
    {
        // Arrange
        var sku1 = Sku.Create("SKU-001");
        var sku2 = Sku.Create("sku-001"); // lowercase, should be uppercase
        var sku3 = Sku.Create("SKU-002");

        // Assert
        sku1.Should().Be(sku2);
        sku1.Should().NotBe(sku3);
    }

    [Fact]
    public void Sku_ImplicitConversion_ShouldReturnValue()
    {
        // Arrange
        var sku = Sku.Create("TEST-SKU");

        // Act
        string value = sku;

        // Assert
        value.Should().Be("TEST-SKU");
    }

    #endregion
}

using BuildingBlocks.Common.Exceptions;
using Order.Domain.ValueObjects;

namespace Order.UnitTests.Domain;

public class ValueObjectTests
{
    #region Address Tests

    [Fact]
    public void Address_Create_WithValidParameters_ShouldCreateAddress()
    {
        // Arrange
        var street = "123 Main St";
        var city = "New York";
        var state = "NY";
        var country = "USA";
        var zipCode = "10001";

        // Act
        var address = Address.Create(street, city, state, country, zipCode);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.Country.Should().Be(country);
        address.ZipCode.Should().Be(zipCode);
    }

    [Fact]
    public void Address_Create_ShouldTrimWhitespace()
    {
        // Act
        var address = Address.Create("  123 Main St  ", "  New York  ", "  NY  ", "  USA  ", "  10001  ");

        // Assert
        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("New York");
        address.State.Should().Be("NY");
        address.Country.Should().Be("USA");
        address.ZipCode.Should().Be("10001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_Create_WithInvalidStreet_ShouldThrowDomainException(string? invalidStreet)
    {
        // Act
        var act = () => Address.Create(invalidStreet!, "City", "State", "Country", "12345");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Street is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_Create_WithInvalidCity_ShouldThrowDomainException(string? invalidCity)
    {
        // Act
        var act = () => Address.Create("Street", invalidCity!, "State", "Country", "12345");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("City is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_Create_WithInvalidState_ShouldThrowDomainException(string? invalidState)
    {
        // Act
        var act = () => Address.Create("Street", "City", invalidState!, "Country", "12345");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("State is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_Create_WithInvalidCountry_ShouldThrowDomainException(string? invalidCountry)
    {
        // Act
        var act = () => Address.Create("Street", "City", "State", invalidCountry!, "12345");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Country is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_Create_WithInvalidZipCode_ShouldThrowDomainException(string? invalidZipCode)
    {
        // Act
        var act = () => Address.Create("Street", "City", "State", "Country", invalidZipCode!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Zip code is required.");
    }

    [Fact]
    public void Address_FullAddress_ShouldFormatCorrectly()
    {
        // Arrange
        var address = Address.Create("123 Main St", "New York", "NY", "USA", "10001");

        // Act
        var fullAddress = address.FullAddress;

        // Assert
        fullAddress.Should().Be("123 Main St, New York, NY 10001, USA");
    }

    [Fact]
    public void Address_Equality_ShouldCompareByAllComponents()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "New York", "NY", "USA", "10001");
        var address2 = Address.Create("123 Main St", "New York", "NY", "USA", "10001");
        var address3 = Address.Create("456 Oak Ave", "New York", "NY", "USA", "10001");

        // Assert
        address1.Should().Be(address2);
        address1.Should().NotBe(address3);
    }

    #endregion

    #region Money Tests

    [Fact]
    public void Money_Create_WithValidParameters_ShouldCreateMoney()
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
            .WithMessage("Amount cannot be negative.");
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
    [InlineData("US")]
    [InlineData("USDD")]
    public void Money_Create_WithInvalidCurrency_ShouldThrowDomainException(string? invalidCurrency)
    {
        // Act
        var act = () => Money.Create(100.00m, invalidCurrency!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Currency must be a valid 3-letter ISO code.");
    }

    [Fact]
    public void Money_Zero_ShouldCreateZeroAmountMoney()
    {
        // Act
        var money = Money.Zero();

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Zero_WithCustomCurrency_ShouldCreateZeroAmountMoney()
    {
        // Act
        var money = Money.Zero("EUR");

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Money_Add_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = Money.Create(50.00m, "USD");
        var money2 = Money.Create(30.00m, "USD");

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(80.00m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Add_WithDifferentCurrencies_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = Money.Create(50.00m, "USD");
        var money2 = Money.Create(30.00m, "EUR");

        // Act
        var act = () => money1.Add(money2);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot add money with different currencies.");
    }

    [Fact]
    public void Money_Multiply_ShouldReturnProduct()
    {
        // Arrange
        var money = Money.Create(25.00m, "USD");

        // Act
        var result = money.Multiply(3);

        // Assert
        result.Amount.Should().Be(75.00m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Multiply_ByZero_ShouldReturnZero()
    {
        // Arrange
        var money = Money.Create(25.00m, "USD");

        // Act
        var result = money.Multiply(0);

        // Assert
        result.Amount.Should().Be(0);
    }

    [Fact]
    public void Money_Multiply_ByNegative_ShouldThrowDomainException()
    {
        // Arrange
        var money = Money.Create(25.00m, "USD");

        // Act
        var act = () => money.Multiply(-1);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Quantity cannot be negative.");
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
}

using Order.Application.Features.Orders.Commands.CreateOrder;

namespace Order.UnitTests.Features.Orders.Commands.CreateOrder;

public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator;

    public CreateOrderValidatorTests()
    {
        _validator = new CreateOrderValidator();
    }

    #region CustomerId Validation

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCustomerId_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CustomerId = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    #endregion

    #region CustomerEmail Validation

    [Fact]
    public void Validate_WithEmptyEmail_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CustomerEmail = "" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CustomerEmail = "invalid-email" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail" && e.ErrorMessage.Contains("Invalid email"));
    }

    #endregion

    #region Address Validation

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyStreet_ShouldHaveError(string? invalidStreet)
    {
        // Arrange
        var command = CreateValidCommand() with { Street = invalidStreet! };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Street");
    }

    [Fact]
    public void Validate_WithLongStreet_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Street = new string('a', 201) };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Street" && e.ErrorMessage.Contains("200"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyCity_ShouldHaveError(string? invalidCity)
    {
        // Arrange
        var command = CreateValidCommand() with { City = invalidCity! };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyState_ShouldHaveError(string? invalidState)
    {
        // Arrange
        var command = CreateValidCommand() with { State = invalidState! };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "State");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyCountry_ShouldHaveError(string? invalidCountry)
    {
        // Arrange
        var command = CreateValidCommand() with { Country = invalidCountry! };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyZipCode_ShouldHaveError(string? invalidZipCode)
    {
        // Arrange
        var command = CreateValidCommand() with { ZipCode = invalidZipCode! };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ZipCode");
    }

    #endregion

    #region Notes Validation

    [Fact]
    public void Validate_WithLongNotes_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = new string('a', 1001) };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void Validate_WithNullNotes_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = null };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Items Validation

    [Fact]
    public void Validate_WithEmptyItems_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Items = [] };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items" && e.ErrorMessage.Contains("At least one item"));
    }

    [Fact]
    public void Validate_WithEmptyProductId_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Items = [new CreateOrderItemDto(Guid.Empty, "Product", 10.00m, "USD", 1)]
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ProductId"));
    }

    [Fact]
    public void Validate_WithEmptyProductName_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Items = [new CreateOrderItemDto(Guid.NewGuid(), "", 10.00m, "USD", 1)]
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ProductName"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WithInvalidUnitPrice_ShouldHaveError(decimal invalidPrice)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Items = [new CreateOrderItemDto(Guid.NewGuid(), "Product", invalidPrice, "USD", 1)]
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("UnitPrice"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Validate_WithInvalidCurrency_ShouldHaveError(string invalidCurrency)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Items = [new CreateOrderItemDto(Guid.NewGuid(), "Product", 10.00m, invalidCurrency, 1)]
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Currency"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidQuantity_ShouldHaveError(int invalidQuantity)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Items = [new CreateOrderItemDto(Guid.NewGuid(), "Product", 10.00m, "USD", invalidQuantity)]
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
    }

    #endregion

    #region Multiple Errors

    [Fact]
    public void Validate_WithMultipleInvalidFields_ShouldHaveMultipleErrors()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.Empty,
            CustomerEmail: "invalid",
            Street: "",
            City: "",
            State: "",
            Country: "",
            ZipCode: "",
            Notes: null,
            Items: []);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(5);
    }

    #endregion

    #region Helper Methods

    private static CreateOrderCommand CreateValidCommand()
    {
        return new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerEmail: "customer@example.com",
            Street: "123 Main St",
            City: "New York",
            State: "NY",
            Country: "USA",
            ZipCode: "10001",
            Notes: null,
            Items:
            [
                new CreateOrderItemDto(Guid.NewGuid(), "Test Product", 99.99m, "USD", 1)
            ]);
    }

    #endregion
}

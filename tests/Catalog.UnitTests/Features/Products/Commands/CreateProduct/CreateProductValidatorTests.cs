using Catalog.Application.Features.Products.Commands.CreateProduct;

namespace Catalog.UnitTests.Features.Products.Commands.CreateProduct;

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator;

    public CreateProductValidatorTests()
    {
        _validator = new CreateProductValidator();
    }

    #region Name Validation

    [Fact]
    public void Validate_WithValidCommand_ShouldBeValid()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = "" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithShortName_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = "AB" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithLongName_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('a', 201) };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    #endregion

    #region Description Validation

    [Fact]
    public void Validate_WithLongDescription_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Description = new string('a', 2001) };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Validate_WithEmptyDescription_ShouldBeValid()
    {
        // Arrange
        var command = CreateValidCommand() with { Description = "" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Price Validation

    [Fact]
    public void Validate_WithZeroPrice_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Price = 0 };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void Validate_WithNegativePrice_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Price = -10.00m };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    #endregion

    #region Currency Validation

    [Fact]
    public void Validate_WithEmptyCurrency_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = "" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void Validate_WithInvalidCurrencyLength_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = "US" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    #endregion

    #region SKU Validation

    [Fact]
    public void Validate_WithEmptySKU_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SKU = "" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public void Validate_WithShortSKU_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SKU = "AB" };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public void Validate_WithLongSKU_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SKU = new string('a', 51) };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    #endregion

    #region StockQuantity Validation

    [Fact]
    public void Validate_WithNegativeStockQuantity_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { StockQuantity = -1 };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StockQuantity");
    }

    [Fact]
    public void Validate_WithZeroStockQuantity_ShouldBeValid()
    {
        // Arrange
        var command = CreateValidCommand() with { StockQuantity = 0 };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region CategoryId Validation

    [Fact]
    public void Validate_WithEmptyCategoryId_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CategoryId = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    #endregion

    #region Helper Methods

    private static CreateProductCommand CreateValidCommand()
    {
        return new CreateProductCommand(
            Name: "Test Product",
            Description: "Test Description",
            Price: 99.99m,
            Currency: "USD",
            SKU: "TEST-SKU-001",
            StockQuantity: 100,
            CategoryId: Guid.NewGuid());
    }

    #endregion
}

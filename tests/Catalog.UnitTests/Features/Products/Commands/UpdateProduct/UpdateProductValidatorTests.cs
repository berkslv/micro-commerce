using Catalog.Application.Features.Products.Commands.UpdateProduct;

namespace Catalog.UnitTests.Features.Products.Commands.UpdateProduct;

public class UpdateProductValidatorTests
{
    private readonly UpdateProductValidator _validator;

    public UpdateProductValidatorTests()
    {
        _validator = new UpdateProductValidator();
    }

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

    #region Id Validation

    [Fact]
    public void Validate_WithEmptyId_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Id = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    #endregion

    #region Name Validation

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

    private static UpdateProductCommand CreateValidCommand()
    {
        return new UpdateProductCommand(
            Id: Guid.NewGuid(),
            Name: "Updated Product",
            Description: "Updated Description",
            Price: 149.99m,
            Currency: "USD",
            StockQuantity: 50,
            CategoryId: Guid.NewGuid());
    }

    #endregion
}

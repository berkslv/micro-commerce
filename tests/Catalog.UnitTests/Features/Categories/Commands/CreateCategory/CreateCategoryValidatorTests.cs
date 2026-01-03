using Catalog.Application.Features.Categories.Commands.CreateCategory;

namespace Catalog.UnitTests.Features.Categories.Commands.CreateCategory;

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator;

    public CreateCategoryValidatorTests()
    {
        _validator = new CreateCategoryValidator();
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
        var command = CreateValidCommand() with { Name = "A" };

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
        var command = CreateValidCommand() with { Name = new string('a', 101) };

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
        var command = CreateValidCommand() with { Description = new string('a', 501) };

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

    #region Helper Methods

    private static CreateCategoryCommand CreateValidCommand()
    {
        return new CreateCategoryCommand(
            Name: "Electronics",
            Description: "Electronic devices and gadgets");
    }

    #endregion
}

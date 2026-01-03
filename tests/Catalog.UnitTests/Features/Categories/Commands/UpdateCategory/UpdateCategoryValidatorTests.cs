using Catalog.Application.Features.Categories.Commands.UpdateCategory;

namespace Catalog.UnitTests.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _validator;

    public UpdateCategoryValidatorTests()
    {
        _validator = new UpdateCategoryValidator();
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

    #endregion

    #region Helper Methods

    private static UpdateCategoryCommand CreateValidCommand()
    {
        return new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Electronics",
            Description: "Electronic devices and gadgets");
    }

    #endregion
}

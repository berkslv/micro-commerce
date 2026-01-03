using Order.Application.Features.Orders.Commands.CancelOrder;

namespace Order.UnitTests.Features.Orders.Commands.CancelOrder;

public class CancelOrderValidatorTests
{
    private readonly CancelOrderValidator _validator;

    public CancelOrderValidatorTests()
    {
        _validator = new CancelOrderValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveError()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.NewGuid(), "Customer requested cancellation");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyOrderId_ShouldHaveError()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.Empty, "Test reason");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyReason_ShouldHaveError(string? invalidReason)
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.NewGuid(), invalidReason!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Validate_WithLongReason_ShouldHaveError()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.NewGuid(), new string('a', 501));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason" && e.ErrorMessage.Contains("500"));
    }

    [Fact]
    public void Validate_WithMultipleInvalidFields_ShouldHaveMultipleErrors()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.Empty, "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}

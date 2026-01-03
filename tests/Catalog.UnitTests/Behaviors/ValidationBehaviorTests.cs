using Catalog.Application.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using ValidationException = BuildingBlocks.Common.Exceptions.ValidationException;

namespace Catalog.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Response Value");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithPassingValidation_ShouldCallNext()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Response Value");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithValidationFailure_ShouldThrowValidationException()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("PropertyName", "Error message")
        };
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test Value");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse("Response Value"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(request, next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithValidationFailure_ShouldNotCallNext()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("PropertyName", "Error message")
        };
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test Value");
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse("Response Value"));
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(request, next, CancellationToken.None));

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldValidateAll()
    {
        // Arrange
        var mockValidator1 = new Mock<IValidator<TestRequest>>();
        mockValidator1
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var mockValidator2 = new Mock<IValidator<TestRequest>>();
        mockValidator2
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { mockValidator1.Object, mockValidator2.Object };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Response Value");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockValidator1.Verify(
            x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockValidator2.Verify(
            x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleFailures_ShouldGroupByPropertyName()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Property1", "Error 1"),
            new("Property1", "Error 2"),
            new("Property2", "Error 3")
        };
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test Value");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse("Response Value"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(request, next, CancellationToken.None));

        exception.Errors.Should().ContainKey("Property1");
        exception.Errors.Should().ContainKey("Property2");
        exception.Errors["Property1"].Should().HaveCount(2);
        exception.Errors["Property2"].Should().HaveCount(1);
    }
}

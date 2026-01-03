using Catalog.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.UnitTests.Behaviors;

public class UnhandledExceptionBehaviorTests
{
    private readonly Mock<ILogger<UnhandledExceptionBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly UnhandledExceptionBehavior<TestRequest, TestResponse> _behavior;

    public UnhandledExceptionBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<UnhandledExceptionBehavior<TestRequest, TestResponse>>>();
        _behavior = new UnhandledExceptionBehavior<TestRequest, TestResponse>(_mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenNoException_ShouldReturnResponse()
    {
        // Arrange
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Response Value");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WhenNoException_ShouldNotLogError()
    {
        // Arrange
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Response Value");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldLogError()
    {
        // Arrange
        var request = new TestRequest("Test Value");
        var expectedException = new InvalidOperationException("Test exception");
        RequestHandlerDelegate<TestResponse> next = (ct) => throw expectedException;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.Handle(request, next, CancellationToken.None));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled Exception")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldRethrowException()
    {
        // Arrange
        var request = new TestRequest("Test Value");
        var expectedException = new InvalidOperationException("Test exception");
        RequestHandlerDelegate<TestResponse> next = (ct) => throw expectedException;

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.Handle(request, next, CancellationToken.None));

        thrownException.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task Handle_ShouldCallNextDelegate()
    {
        // Arrange
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Response Value");
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }
}

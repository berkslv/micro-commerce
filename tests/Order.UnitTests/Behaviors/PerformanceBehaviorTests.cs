using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Behaviors;

namespace Order.UnitTests.Behaviors;

public class PerformanceBehaviorTests
{
    private readonly Mock<ILogger<PerformanceBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly PerformanceBehavior<TestRequest, TestResponse> _behavior;

    public PerformanceBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<PerformanceBehavior<TestRequest, TestResponse>>>();
        _behavior = new PerformanceBehavior<TestRequest, TestResponse>(_mockLogger.Object);
    }

    [Fact]
    public async Task Handle_FastRequest_ShouldNotLogWarning()
    {
        // Arrange
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Response Value");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SlowRequest_ShouldLogWarning()
    {
        // Arrange
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Response Value");
        RequestHandlerDelegate<TestResponse> next = async (ct) =>
        {
            await Task.Delay(600); // Delay longer than 500ms threshold
            return expectedResponse;
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Long Running Request")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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

    [Fact]
    public async Task Handle_ShouldReturnResponseFromNextDelegate()
    {
        // Arrange
        var request = new TestRequest("Test Value");
        var expectedResponse = new TestResponse("Expected Response");
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }
}

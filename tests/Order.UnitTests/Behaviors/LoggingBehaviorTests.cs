using BuildingBlocks.Observability.Metrics;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Behaviors;
using System.Diagnostics.Metrics;

namespace Order.UnitTests.Behaviors;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly MediatRMetrics _metrics;
    private readonly LoggingBehavior<TestRequest, TestResponse> _behavior;

    public LoggingBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var meterFactory = new TestMeterFactory();
        _metrics = new MediatRMetrics(meterFactory);
        _behavior = new LoggingBehavior<TestRequest, TestResponse>(_mockLogger.Object, _metrics);
    }

    [Fact]
    public async Task Handle_ShouldLogRequestBeforeHandling()
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
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogResponseAfterHandling()
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
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled")),
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

/// <summary>
/// Test implementation of IMeterFactory for unit testing
/// </summary>
public sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = new();

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in _meters)
        {
            meter.Dispose();
        }
        _meters.Clear();
    }
}

// Test helper classes
public sealed record TestRequest(string Value) : IRequest<TestResponse>;
public sealed record TestResponse(string Value);

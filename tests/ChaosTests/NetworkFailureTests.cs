using ChaosTests.Infrastructure;
using Polly;
using System.Diagnostics;

namespace ChaosTests;

/// <summary>
/// Tests for network failure scenarios between services.
/// Verifies retry policies, timeouts, and graceful degradation.
/// </summary>
[TestFixture]
[Category("Chaos")]
public class NetworkFailureTests
{
    private HttpClient _catalogClient = null!;
    private HttpClient _orderClient = null!;
    private ResilienceMetrics _metrics = null!;

    [SetUp]
    public void Setup()
    {
        _catalogClient = Testing.CatalogClient;
        _orderClient = Testing.OrderClient;
        _metrics = Testing.Metrics;
        Testing.ResetMetrics();
    }

    [Test]
    public async Task ShouldHandleHighLatencyGracefully()
    {
        // Arrange
        var latencyPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromMilliseconds(500),
            injectionRate: 0.5);
        
        var stopwatch = new Stopwatch();
        var responseTimes = new List<long>();
        
        // Act - Make multiple requests with injected latency
        for (int i = 0; i < 10; i++)
        {
            stopwatch.Restart();
            
            var response = await latencyPolicy.ExecuteAsync(async token =>
            {
                return await _catalogClient.GetAsync("/health", token);
            }, CancellationToken.None);
            
            stopwatch.Stop();
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
            
            _metrics.RecordRequest();
            if (response.IsSuccessStatusCode)
                _metrics.RecordSuccess();
            else
                _metrics.RecordFailure();
        }

        // Assert
        _metrics.SuccessfulRequests.Should().Be(10, "all health check requests should succeed even with latency");
        responseTimes.Average().Should().BeGreaterThan(100, "some requests should have injected latency");
        
        Console.WriteLine($"Response times (ms): {string.Join(", ", responseTimes)}");
        Console.WriteLine($"Average response time: {responseTimes.Average():F0}ms");
        Console.WriteLine(_metrics.ToString());
    }

    [Test]
    public async Task ShouldRetryOnTransientNetworkFailures()
    {
        // Arrange
        var retryPolicy = ResiliencePolicies.CreateRetryPolicy(retryCount: 3);
        var faultPolicy = ChaosStrategies.CreateNetworkFailureStrategy(injectionRate: 0.3);
        
        int successCount = 0;
        int failureCount = 0;

        // Act - Make multiple requests with potential failures and retry
        for (int i = 0; i < 10; i++)
        {
            _metrics.RecordRequest();
            
            try
            {
                // First apply fault injection, then retry
                var response = await retryPolicy.ExecuteAsync(async token =>
                {
                    return await faultPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/health", innerToken);
                    }, token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                    _metrics.RecordSuccess();
                }
                else
                {
                    failureCount++;
                    _metrics.RecordFailure();
                }
            }
            catch (HttpRequestException)
            {
                failureCount++;
                _metrics.RecordFailure();
            }
        }

        // Assert - Retry policy should help recover from many failures
        Console.WriteLine($"Success: {successCount}, Failures: {failureCount}");
        Console.WriteLine(_metrics.ToString());
        
        // With 30% fault rate and 3 retries, most requests should eventually succeed
        successCount.Should().BeGreaterThanOrEqualTo(5, "retry policy should help recover from transient failures");
    }

    [Test]
    public async Task ShouldTimeoutOnSlowResponses()
    {
        // Arrange
        var timeoutPolicy = ResiliencePolicies.CreateTimeoutPolicy(timeout: TimeSpan.FromMilliseconds(200));
        var latencyPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromSeconds(2),
            injectionRate: 0.7);
        
        int timeoutCount = 0;
        int successCount = 0;

        // Act
        for (int i = 0; i < 10; i++)
        {
            _metrics.RecordRequest();
            
            try
            {
                var response = await timeoutPolicy.ExecuteAsync(async token =>
                {
                    return await latencyPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/health", innerToken);
                    }, token);
                }, CancellationToken.None);
                
                successCount++;
                _metrics.RecordSuccess();
            }
            catch (TimeoutRejectedException)
            {
                timeoutCount++;
                _metrics.RecordTimeout();
                _metrics.RecordFailure();
            }
        }

        // Assert - Should see some timeouts due to high latency injection
        Console.WriteLine($"Success: {successCount}, Timeouts: {timeoutCount}");
        Console.WriteLine(_metrics.ToString());
        
        // With 70% latency rate of 2s and 200ms timeout, we should see timeouts
        timeoutCount.Should().BeGreaterThan(0, "should timeout on slow responses");
    }

    [Test]
    public async Task ShouldHandleConcurrentRequestsUnderNetworkStress()
    {
        // Arrange
        var chaosPolicy = ChaosStrategies.CreateCombinedChaosStrategy(
            latencyRate: 0.2,
            faultRate: 0.1,
            latency: TimeSpan.FromMilliseconds(200));
        
        var tasks = new List<Task<bool>>();
        
        // Act - Send concurrent requests
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(MakeRequestWithChaos(chaosPolicy));
        }
        
        var results = await Task.WhenAll(tasks);
        
        int successCount = results.Count(r => r);
        int failureCount = results.Count(r => !r);

        // Assert
        Console.WriteLine($"Concurrent requests - Success: {successCount}, Failures: {failureCount}");
        
        // Most concurrent requests should succeed despite chaos
        successCount.Should().BeGreaterThanOrEqualTo(10, "system should handle concurrent requests under stress");
    }

    [Test]
    public async Task ShouldRecoverAfterNetworkPartition()
    {
        // Arrange - Simulate network partition (100% failure rate)
        var partitionPolicy = ChaosStrategies.CreateNetworkFailureStrategy(injectionRate: 1.0);
        
        // During partition - all requests fail
        int failuresDuringPartition = 0;
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await partitionPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
            }
            catch (HttpRequestException)
            {
                failuresDuringPartition++;
            }
        }

        // Simulate network recovery (no faults)
        int successesAfterRecovery = 0;
        for (int i = 0; i < 5; i++)
        {
            var response = await _catalogClient.GetAsync("/health");
            if (response.IsSuccessStatusCode)
                successesAfterRecovery++;
        }

        // Assert
        Console.WriteLine($"Failures during partition: {failuresDuringPartition}");
        Console.WriteLine($"Successes after recovery: {successesAfterRecovery}");
        
        failuresDuringPartition.Should().Be(5, "all requests should fail during partition");
        successesAfterRecovery.Should().Be(5, "all requests should succeed after network recovery");
    }

    [Test]
    public async Task ShouldMeasureServiceRecoveryTime()
    {
        // Arrange
        var faultPolicy = ChaosStrategies.CreateNetworkFailureStrategy(injectionRate: 0.8);
        var stopwatch = new Stopwatch();
        
        // Act - Measure time to first successful request after failures
        stopwatch.Start();
        
        int attempts = 0;
        bool recovered = false;
        
        while (!recovered && attempts < 20)
        {
            attempts++;
            try
            {
                var response = await faultPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                {
                    recovered = true;
                }
            }
            catch (HttpRequestException)
            {
                // Expected during chaos
            }
            
            await Task.Delay(10); // Small delay between attempts
        }
        
        stopwatch.Stop();

        // Assert
        Console.WriteLine($"Recovery achieved: {recovered}, Attempts: {attempts}, Time: {stopwatch.ElapsedMilliseconds}ms");
        
        recovered.Should().BeTrue("service should eventually recover even with 80% fault rate");
        attempts.Should().BeLessThan(20, "should recover within reasonable attempts");
    }

    private async Task<bool> MakeRequestWithChaos(ResiliencePipeline<HttpResponseMessage> chaosPolicy)
    {
        try
        {
            _metrics.RecordRequest();
            
            var response = await chaosPolicy.ExecuteAsync(async token =>
            {
                return await _catalogClient.GetAsync("/health", token);
            }, CancellationToken.None);
            
            if (response.IsSuccessStatusCode)
            {
                _metrics.RecordSuccess();
                return true;
            }
            
            _metrics.RecordFailure();
            return false;
        }
        catch
        {
            _metrics.RecordFailure();
            return false;
        }
    }
}

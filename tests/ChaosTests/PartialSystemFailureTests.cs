using ChaosTests.Infrastructure;
using Polly;
using System.Diagnostics;

namespace ChaosTests;

/// <summary>
/// Tests for system behavior under partial failures.
/// Verifies graceful degradation when some components fail.
/// </summary>
[TestFixture]
[Category("Chaos")]
public class PartialSystemFailureTests
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
    public async Task ShouldDegradeGracefullyWhenCatalogServiceIsSlow()
    {
        // Arrange
        var latencyPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromSeconds(1),
            injectionRate: 0.5);
        
        var timeoutPolicy = ResiliencePolicies.CreateTimeoutPolicy(
            timeout: TimeSpan.FromSeconds(2));
        
        int catalogSuccesses = 0;
        int catalogTimeouts = 0;

        // Act - Access Catalog service with injected latency
        for (int i = 0; i < 10; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var response = await timeoutPolicy.ExecuteAsync(async token =>
                {
                    return await latencyPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/api/categories", innerToken);
                    }, token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                    catalogSuccesses++;
            }
            catch (TimeoutRejectedException)
            {
                catalogTimeouts++;
            }
            
            stopwatch.Stop();
            Console.WriteLine($"Request {i + 1}: {stopwatch.ElapsedMilliseconds}ms");
        }

        // Meanwhile, verify Order service is unaffected
        var orderResponse = await _orderClient.GetAsync("/health");
        var orderHealthy = orderResponse.IsSuccessStatusCode;

        // Assert
        Console.WriteLine($"Catalog - Success: {catalogSuccesses}, Timeouts: {catalogTimeouts}");
        Console.WriteLine($"Order service healthy: {orderHealthy}");
        
        catalogSuccesses.Should().BeGreaterThan(0, "some catalog requests should succeed");
        orderHealthy.Should().BeTrue("Order service should remain healthy when Catalog is slow");
    }

    [Test]
    public async Task ShouldHandlePartialServiceFailures()
    {
        // Arrange - Catalog has intermittent failures
        var catalogFaultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.ServiceUnavailable,
            injectionRate: 0.4);
        
        // Order service has no faults
        int catalogSuccesses = 0;
        int catalogFailures = 0;
        int orderSuccesses = 0;

        // Act
        var tasks = new List<Task>();
        
        // Concurrent catalog requests with faults
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var response = await catalogFaultPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/api/categories", token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                    Interlocked.Increment(ref catalogSuccesses);
                else
                    Interlocked.Increment(ref catalogFailures);
            }));
        }
        
        // Concurrent order requests without faults
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var response = await _orderClient.GetAsync("/health");
                if (response.IsSuccessStatusCode)
                    Interlocked.Increment(ref orderSuccesses);
            }));
        }
        
        await Task.WhenAll(tasks);

        // Assert
        Console.WriteLine($"Catalog - Success: {catalogSuccesses}, Failures: {catalogFailures}");
        Console.WriteLine($"Order - Success: {orderSuccesses}");
        
        catalogSuccesses.Should().BeGreaterThan(0, "some catalog requests should succeed");
        orderSuccesses.Should().Be(10, "all order health checks should succeed");
    }

    [Test]
    public async Task ShouldIsolateFaultsBetweenServices()
    {
        // Arrange - Catalog service completely down
        var catalogDownPolicy = ChaosStrategies.CreateNetworkFailureStrategy(injectionRate: 1.0);
        
        // Act - Verify Catalog is "down"
        int catalogFailures = 0;
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await catalogDownPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
            }
            catch
            {
                catalogFailures++;
            }
        }
        
        // Verify Order service is completely unaffected
        var orderResponses = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            var response = await _orderClient.GetAsync("/health");
            orderResponses.Add(response.IsSuccessStatusCode);
        }

        // Assert
        Console.WriteLine($"Catalog failures (simulated down): {catalogFailures}");
        Console.WriteLine($"Order successes: {orderResponses.Count(r => r)}");
        
        catalogFailures.Should().Be(5, "all catalog requests should fail when down");
        orderResponses.All(r => r).Should().BeTrue("Order service should be isolated from Catalog failures");
    }

    [Test]
    public async Task ShouldMaintainServiceAvailabilityDuringDegradation()
    {
        // Arrange
        var chaosPolicy = ChaosStrategies.CreateCombinedChaosStrategy(
            latencyRate: 0.3,
            faultRate: 0.2,
            latency: TimeSpan.FromMilliseconds(500));
        
        var retryPolicy = ResiliencePolicies.CreateRetryPolicy(retryCount: 2);
        
        var stopwatch = Stopwatch.StartNew();
        var successfulOperations = 0;
        var failedOperations = 0;

        // Act - Perform mixed operations under degradation
        for (int i = 0; i < 20; i++)
        {
            try
            {
                var response = await retryPolicy.ExecuteAsync(async token =>
                {
                    return await chaosPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/health", innerToken);
                    }, token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                    successfulOperations++;
                else
                    failedOperations++;
            }
            catch
            {
                failedOperations++;
            }
        }
        
        stopwatch.Stop();
        var totalTime = stopwatch.ElapsedMilliseconds;
        var averageTime = totalTime / 20.0;

        // Assert
        var successRate = (double)successfulOperations / 20 * 100;
        
        Console.WriteLine($"Success: {successfulOperations}, Failed: {failedOperations}");
        Console.WriteLine($"Success rate: {successRate:F1}%");
        Console.WriteLine($"Total time: {totalTime}ms, Average: {averageTime:F1}ms per request");
        
        // With retry policy, success rate should be good despite chaos
        successRate.Should().BeGreaterThanOrEqualTo(60, "system should maintain >60% availability with retries");
    }

    [Test]
    public async Task ShouldProvideHealthStatusDuringPartialFailure()
    {
        // Arrange
        var partialFailurePolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 0.5);

        // Act - Check health endpoints during failures
        var catalogHealthStatuses = new List<(bool Success, int? StatusCode)>();
        var orderHealthStatuses = new List<(bool Success, int? StatusCode)>();
        
        for (int i = 0; i < 10; i++)
        {
            // Catalog with failures
            try
            {
                var response = await partialFailurePolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
                
                catalogHealthStatuses.Add((response.IsSuccessStatusCode, (int)response.StatusCode));
            }
            catch
            {
                catalogHealthStatuses.Add((false, null));
            }
            
            // Order without failures
            var orderResponse = await _orderClient.GetAsync("/health");
            orderHealthStatuses.Add((orderResponse.IsSuccessStatusCode, (int)orderResponse.StatusCode));
        }

        // Assert
        var catalogHealthy = catalogHealthStatuses.Count(s => s.Success);
        var orderHealthy = orderHealthStatuses.Count(s => s.Success);
        
        Console.WriteLine($"Catalog health checks passed: {catalogHealthy}/10");
        Console.WriteLine($"Order health checks passed: {orderHealthy}/10");
        
        catalogHealthy.Should().BeGreaterThan(0, "some catalog health checks should pass");
        orderHealthy.Should().Be(10, "all order health checks should pass");
    }

    [Test]
    public async Task ShouldMeasureDegradationImpact()
    {
        // Arrange - Measure baseline performance
        var baselineResponses = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var sw = Stopwatch.StartNew();
            await _catalogClient.GetAsync("/health");
            sw.Stop();
            baselineResponses.Add(sw.ElapsedMilliseconds);
        }
        
        var baselineAverage = baselineResponses.Average();
        Console.WriteLine($"Baseline average: {baselineAverage:F1}ms");
        
        // Apply degradation
        var degradationPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromMilliseconds(200),
            injectionRate: 0.6);
        
        var degradedResponses = new List<long>();
        for (int i = 0; i < 10; i++)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await degradationPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
            }
            catch { }
            sw.Stop();
            degradedResponses.Add(sw.ElapsedMilliseconds);
        }
        
        var degradedAverage = degradedResponses.Average();

        // Assert
        Console.WriteLine($"Degraded average: {degradedAverage:F1}ms");
        Console.WriteLine($"Performance impact: {(degradedAverage / baselineAverage - 1) * 100:F1}% slower");
        
        // Performance should be measurably impacted but still functional
        degradedAverage.Should().BeGreaterThan(baselineAverage);
    }

    [Test]
    public async Task ShouldRecoverFromCascadingFailures()
    {
        // Arrange - Simulate cascading failure scenario
        var cascadePolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 0.9);
        
        // Phase 1: Cascading failures (90% failure rate)
        Console.WriteLine("Phase 1: Cascading failures");
        int phase1Failures = 0;
        for (int i = 0; i < 10; i++)
        {
            var response = await cascadePolicy.ExecuteAsync(async token =>
            {
                return await _catalogClient.GetAsync("/health", token);
            }, CancellationToken.None);
            
            if (!response.IsSuccessStatusCode)
                phase1Failures++;
        }
        
        Console.WriteLine($"Phase 1 failures: {phase1Failures}/10");

        // Phase 2: Recovery (no failures)
        Console.WriteLine("Phase 2: Recovery");
        int phase2Successes = 0;
        for (int i = 0; i < 10; i++)
        {
            var response = await _catalogClient.GetAsync("/health");
            if (response.IsSuccessStatusCode)
                phase2Successes++;
        }
        
        Console.WriteLine($"Phase 2 successes: {phase2Successes}/10");

        // Assert
        phase1Failures.Should().BeGreaterThanOrEqualTo(7, "phase 1 should have high failure rate");
        phase2Successes.Should().Be(10, "phase 2 should fully recover");
    }
}

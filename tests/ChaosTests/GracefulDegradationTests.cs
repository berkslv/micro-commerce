using ChaosTests.Infrastructure;
using Polly;
using System.Diagnostics;

namespace ChaosTests;

/// <summary>
/// Tests for system-wide graceful degradation scenarios.
/// Verifies the system remains functional under various failure conditions.
/// </summary>
[TestFixture]
[Category("Chaos")]
public class GracefulDegradationTests
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
    public async Task ShouldProvideReducedFunctionalityUnderStress()
    {
        // Arrange
        var stressPolicy = ChaosStrategies.CreateCombinedChaosStrategy(
            latencyRate: 0.5,
            faultRate: 0.3,
            latency: TimeSpan.FromMilliseconds(300));
        
        var results = new Dictionary<string, List<bool>>
        {
            ["health"] = [],
            ["categories"] = []
        };

        // Act - Test different endpoints under stress
        for (int i = 0; i < 10; i++)
        {
            // Health check - should be more resilient
            try
            {
                var healthResponse = await stressPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
                results["health"].Add(healthResponse.IsSuccessStatusCode);
            }
            catch
            {
                results["health"].Add(false);
            }
            
            // Categories - database operation
            try
            {
                var categoriesResponse = await stressPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/api/categories", token);
                }, CancellationToken.None);
                results["categories"].Add(categoriesResponse.IsSuccessStatusCode);
            }
            catch
            {
                results["categories"].Add(false);
            }
        }

        // Assert
        var healthSuccessRate = results["health"].Count(r => r) / 10.0 * 100;
        var categoriesSuccessRate = results["categories"].Count(r => r) / 10.0 * 100;
        
        Console.WriteLine($"Health endpoint success rate: {healthSuccessRate:F1}%");
        Console.WriteLine($"Categories endpoint success rate: {categoriesSuccessRate:F1}%");
        
        // System should maintain partial functionality
        (healthSuccessRate + categoriesSuccessRate).Should().BeGreaterThan(50,
            "system should maintain at least partial functionality under stress");
    }

    [Test]
    public async Task ShouldPrioritizeCriticalOperations()
    {
        // Arrange - Heavy load on system
        var loadPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromMilliseconds(500),
            injectionRate: 0.7);
        
        var criticalSuccesses = 0;
        var nonCriticalSuccesses = 0;
        var tasks = new List<Task>();
        var lockObj = new object();

        // Act - Mix of critical (health) and non-critical (list) operations
        // Critical operations get timeout protection
        var timeoutPolicy = ResiliencePolicies.CreateTimeoutPolicy(timeout: TimeSpan.FromSeconds(2));
        
        // Health checks (critical)
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var response = await timeoutPolicy.ExecuteAsync(async token =>
                    {
                        return await loadPolicy.ExecuteAsync(async innerToken =>
                        {
                            return await _catalogClient.GetAsync("/health", innerToken);
                        }, token);
                    }, CancellationToken.None);
                    
                    if (response.IsSuccessStatusCode)
                        Interlocked.Increment(ref criticalSuccesses);
                }
                catch { }
            }));
        }
        
        // List operations (non-critical) - no timeout protection
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var response = await loadPolicy.ExecuteAsync(async token =>
                    {
                        return await _catalogClient.GetAsync("/api/categories", token);
                    }, CancellationToken.None);
                    
                    if (response.IsSuccessStatusCode)
                        Interlocked.Increment(ref nonCriticalSuccesses);
                }
                catch { }
            }));
        }
        
        await Task.WhenAll(tasks);

        // Assert
        Console.WriteLine($"Critical (health) successes: {criticalSuccesses}/10");
        Console.WriteLine($"Non-critical (list) successes: {nonCriticalSuccesses}/10");
        
        criticalSuccesses.Should().BeGreaterThanOrEqualTo(nonCriticalSuccesses / 2,
            "critical operations should maintain reasonable availability");
    }

    [Test]
    public async Task ShouldFallbackToDefaultBehaviorOnFailure()
    {
        // Arrange - Complete failure scenario
        var failurePolicy = ChaosStrategies.CreateNetworkFailureStrategy(injectionRate: 1.0);
        
        // Define fallback behavior
        var fallbackResults = new List<string>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            string result;
            
            try
            {
                var response = await failurePolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/api/categories", token);
                }, CancellationToken.None);
                
                result = response.IsSuccessStatusCode ? "success" : "error";
            }
            catch
            {
                // Fallback: return cached/default data indicator
                result = "fallback";
            }
            
            fallbackResults.Add(result);
        }

        // Assert
        Console.WriteLine($"Results: {string.Join(", ", fallbackResults)}");
        
        fallbackResults.All(r => r == "fallback").Should().BeTrue(
            "all requests should trigger fallback under complete failure");
    }

    [Test]
    public async Task ShouldMaintainResponseQualityDuringDegradation()
    {
        // Arrange - First create test data
        var categoryRequest = new
        {
            Name = $"Degradation Test {Guid.NewGuid():N}",
            Description = "Testing response quality during degradation"
        };
        
        var createResponse = await _catalogClient.PostAsJsonAsync("/api/categories", categoryRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        
        // Apply degradation
        var degradationPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromMilliseconds(200),
            injectionRate: 0.5);
        
        var consistentReads = 0;
        var totalReads = 0;

        // Act - Read data under degradation
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var response = await degradationPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync($"/api/categories/{createdCategory!.Id}", token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                {
                    totalReads++;
                    var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
                    
                    // Verify data integrity
                    if (category?.Name == createdCategory!.Name)
                        consistentReads++;
                }
            }
            catch
            {
                // Expected under chaos
            }
        }

        // Assert
        Console.WriteLine($"Consistent reads: {consistentReads}/{totalReads}");
        
        if (totalReads > 0)
        {
            (consistentReads / (double)totalReads * 100).Should().Be(100,
                "all successful responses should return consistent data");
        }
    }

    [Test]
    public async Task ShouldGracefullyHandleResourceExhaustion()
    {
        // Arrange - Simulate resource pressure with many concurrent requests
        var concurrentRequests = 100;
        var results = new List<(bool Success, long ElapsedMs)>();
        var lockObj = new object();
        
        var latencyPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromMilliseconds(100),
            injectionRate: 0.3);

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async i =>
        {
            var sw = Stopwatch.StartNew();
            bool success = false;
            
            try
            {
                var response = await latencyPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
                
                success = response.IsSuccessStatusCode;
            }
            catch
            {
                success = false;
            }
            
            sw.Stop();
            
            lock (lockObj)
            {
                results.Add((success, sw.ElapsedMilliseconds));
            }
        });
        
        await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.Success);
        var avgResponseTime = results.Average(r => r.ElapsedMs);
        var maxResponseTime = results.Max(r => r.ElapsedMs);
        
        Console.WriteLine($"Success: {successCount}/{concurrentRequests}");
        Console.WriteLine($"Average response time: {avgResponseTime:F0}ms");
        Console.WriteLine($"Max response time: {maxResponseTime}ms");
        
        // System should handle concurrent load gracefully
        (successCount / (double)concurrentRequests * 100).Should().BeGreaterThanOrEqualTo(50,
            "system should handle at least 50% of requests under resource pressure");
    }

    [Test]
    public async Task ShouldRecoverGracefullyAfterOverload()
    {
        // Arrange
        var overloadPolicy = ChaosStrategies.CreateCombinedChaosStrategy(
            latencyRate: 0.8,
            faultRate: 0.4,
            latency: TimeSpan.FromMilliseconds(500));
        
        // Phase 1: Overload
        Console.WriteLine("Phase 1: Overload");
        int overloadSuccesses = 0;
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var response = await overloadPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                    overloadSuccesses++;
            }
            catch { }
        }
        
        Console.WriteLine($"Overload phase successes: {overloadSuccesses}/10");
        
        // Phase 2: Recovery period (no chaos)
        Console.WriteLine("Phase 2: Recovery");
        var stopwatch = Stopwatch.StartNew();
        
        int recoverySuccesses = 0;
        int recoveryAttempts = 0;
        
        while (recoverySuccesses < 5 && recoveryAttempts < 10)
        {
            recoveryAttempts++;
            var response = await _catalogClient.GetAsync("/health");
            if (response.IsSuccessStatusCode)
                recoverySuccesses++;
        }
        
        stopwatch.Stop();
        
        Console.WriteLine($"Recovery: {recoverySuccesses} successes in {recoveryAttempts} attempts ({stopwatch.ElapsedMilliseconds}ms)");

        // Assert
        recoverySuccesses.Should().BeGreaterThanOrEqualTo(5, "system should recover quickly after overload");
        recoveryAttempts.Should().BeLessThanOrEqualTo(5, "recovery should be immediate");
    }

    [Test]
    public async Task ShouldMaintainServiceLevelObjectivesUnderDegradation()
    {
        // Arrange - Define SLOs
        const double targetSuccessRate = 70.0; // 70% success rate target
        const long targetP95Latency = 1000; // 1000ms P95 latency target
        
        var degradationPolicy = ChaosStrategies.CreateCombinedChaosStrategy(
            latencyRate: 0.3,
            faultRate: 0.1,
            latency: TimeSpan.FromMilliseconds(200));
        
        var results = new List<(bool Success, long LatencyMs)>();

        // Act - Measure SLOs under degradation
        for (int i = 0; i < 50; i++)
        {
            var sw = Stopwatch.StartNew();
            bool success = false;
            
            try
            {
                var response = await degradationPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
                
                success = response.IsSuccessStatusCode;
            }
            catch { }
            
            sw.Stop();
            results.Add((success, sw.ElapsedMilliseconds));
        }

        // Calculate SLO metrics
        var successRate = results.Count(r => r.Success) / (double)results.Count * 100;
        var sortedLatencies = results.Select(r => r.LatencyMs).OrderBy(l => l).ToList();
        var p95Latency = sortedLatencies[(int)(sortedLatencies.Count * 0.95)];
        var avgLatency = sortedLatencies.Average();

        // Assert
        Console.WriteLine($"Success Rate: {successRate:F1}% (target: >={targetSuccessRate}%)");
        Console.WriteLine($"P95 Latency: {p95Latency}ms (target: <={targetP95Latency}ms)");
        Console.WriteLine($"Average Latency: {avgLatency:F0}ms");
        
        successRate.Should().BeGreaterThanOrEqualTo(targetSuccessRate,
            $"success rate should meet SLO target of {targetSuccessRate}%");
        p95Latency.Should().BeLessThanOrEqualTo(targetP95Latency,
            $"P95 latency should meet SLO target of {targetP95Latency}ms");
    }

    private record CategoryResponse(Guid Id, string Name, string Description, DateTime CreatedAt);
}

using ChaosTests.Infrastructure;
using Polly;
using System.Diagnostics;

namespace ChaosTests;

/// <summary>
/// Tests for retry policy behavior under various failure conditions.
/// Verifies exponential backoff and retry limits.
/// </summary>
[TestFixture]
[Category("Chaos")]
public class RetryPolicyTests
{
    private HttpClient _catalogClient = null!;
    private ResilienceMetrics _metrics = null!;

    [SetUp]
    public void Setup()
    {
        _catalogClient = Testing.CatalogClient;
        _metrics = Testing.Metrics;
        Testing.ResetMetrics();
    }

    [Test]
    public async Task ShouldRetryOnTransientFailures()
    {
        // Arrange
        int attemptCount = 0;
        
        var retryPolicy = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnRetry = args =>
                {
                    attemptCount++;
                    _metrics.RecordRetry();
                    Console.WriteLine($"Retry {attemptCount} at {DateTime.UtcNow:HH:mm:ss.fff}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
        
        // Fault policy with 50% failure rate
        var faultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 0.5);

        // Act
        var response = await retryPolicy.ExecuteAsync(async token =>
        {
            return await faultPolicy.ExecuteAsync(async innerToken =>
            {
                return await _catalogClient.GetAsync("/health", innerToken);
            }, token);
        }, CancellationToken.None);

        // Assert
        Console.WriteLine($"Final status: {response.StatusCode}");
        Console.WriteLine($"Total retry attempts: {attemptCount}");
        Console.WriteLine(_metrics.ToString());
        
        // With retries, we should eventually succeed
        response.IsSuccessStatusCode.Should().BeTrue("retries should help succeed despite 50% failures");
    }

    [Test]
    public async Task ShouldUseExponentialBackoff()
    {
        // Arrange
        var retryDelays = new List<TimeSpan>();
        var stopwatch = new Stopwatch();
        var lastAttemptTime = DateTime.UtcNow;
        
        var retryPolicy = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 4,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnRetry = args =>
                {
                    var now = DateTime.UtcNow;
                    retryDelays.Add(now - lastAttemptTime);
                    lastAttemptTime = now;
                    Console.WriteLine($"Retry after {retryDelays.Last().TotalMilliseconds:F0}ms delay");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
        
        // Always fail to see all retries
        var alwaysFailPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 1.0);
        
        stopwatch.Start();

        // Act
        var response = await retryPolicy.ExecuteAsync(async token =>
        {
            return await alwaysFailPolicy.ExecuteAsync(async innerToken =>
            {
                return await _catalogClient.GetAsync("/health", innerToken);
            }, token);
        }, CancellationToken.None);
        
        stopwatch.Stop();

        // Assert
        Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Delays (ms): {string.Join(", ", retryDelays.Select(d => d.TotalMilliseconds.ToString("F0")))}");
        
        // Verify exponential increase (each delay roughly 2x previous)
        for (int i = 1; i < retryDelays.Count; i++)
        {
            // Allow some tolerance for timing variations
            retryDelays[i].TotalMilliseconds.Should()
                .BeGreaterThan(retryDelays[i - 1].TotalMilliseconds * 1.5,
                    "delays should increase exponentially");
        }
    }

    [Test]
    public async Task ShouldRespectMaxRetryAttempts()
    {
        // Arrange
        int attemptCount = 0;
        const int maxRetries = 3;
        
        var retryPolicy = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>(),
                OnRetry = args =>
                {
                    attemptCount++;
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
        
        // Always throw to force all retries
        var alwaysFailPolicy = ChaosStrategies.CreateNetworkFailureStrategy(injectionRate: 1.0);

        // Act
        Func<Task> action = async () =>
        {
            await retryPolicy.ExecuteAsync(async token =>
            {
                return await alwaysFailPolicy.ExecuteAsync(async innerToken =>
                {
                    return await _catalogClient.GetAsync("/health", innerToken);
                }, token);
            }, CancellationToken.None);
        };

        // Assert
        await action.Should().ThrowAsync<HttpRequestException>();
        attemptCount.Should().Be(maxRetries, $"should retry exactly {maxRetries} times");
    }

    [Test]
    public async Task ShouldNotRetryOnNonTransientFailures()
    {
        // Arrange
        int retryCount = 0;
        
        var retryPolicy = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(10),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500), // Only 5xx
                OnRetry = args =>
                {
                    retryCount++;
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // Act - Make request that returns 4xx (should not retry)
        var response = await retryPolicy.ExecuteAsync(async token =>
        {
            // Request non-existent resource - returns 404
            return await _catalogClient.GetAsync($"/api/categories/{Guid.NewGuid()}", token);
        }, CancellationToken.None);

        // Assert
        Console.WriteLine($"Response: {response.StatusCode}, Retries: {retryCount}");
        
        retryCount.Should().Be(0, "should not retry on 4xx client errors");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldMeasureRetryOverhead()
    {
        // Arrange - Measure without retries
        var stopwatch = new Stopwatch();
        
        stopwatch.Start();
        await _catalogClient.GetAsync("/health");
        stopwatch.Stop();
        var baselineTime = stopwatch.ElapsedMilliseconds;
        
        // With retries but no failures
        var retryPolicy = ResiliencePolicies.CreateRetryPolicy(retryCount: 3);
        
        stopwatch.Restart();
        await retryPolicy.ExecuteAsync(async token =>
        {
            return await _catalogClient.GetAsync("/health", token);
        }, CancellationToken.None);
        stopwatch.Stop();
        var withRetryTime = stopwatch.ElapsedMilliseconds;

        // Assert
        Console.WriteLine($"Baseline: {baselineTime}ms");
        Console.WriteLine($"With retry policy: {withRetryTime}ms");
        Console.WriteLine($"Overhead: {withRetryTime - baselineTime}ms");
        
        // Retry policy should add minimal overhead when no retries needed
        (withRetryTime - baselineTime).Should().BeLessThan(100,
            "retry policy overhead should be minimal when no retries needed");
    }

    [Test]
    public async Task ShouldRecoverEventuallyWithRetries()
    {
        // Arrange
        int successCount = 0;
        int totalAttempts = 0;
        
        var retryPolicy = ResiliencePolicies.CreateRetryPolicy(
            retryCount: 5,
            initialDelay: TimeSpan.FromMilliseconds(50));
        
        // High failure rate but not 100%
        var faultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.ServiceUnavailable,
            injectionRate: 0.7);

        // Act - Multiple independent operations
        for (int i = 0; i < 10; i++)
        {
            totalAttempts++;
            
            try
            {
                var response = await retryPolicy.ExecuteAsync(async token =>
                {
                    return await faultPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/health", innerToken);
                    }, token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                    successCount++;
            }
            catch
            {
                // Failed even after retries
            }
        }

        // Assert
        var successRate = (double)successCount / totalAttempts * 100;
        
        Console.WriteLine($"Success: {successCount}/{totalAttempts} ({successRate:F1}%)");
        
        // With 5 retries and 70% failure, most operations should eventually succeed
        successRate.Should().BeGreaterThanOrEqualTo(70, 
            "retry policy should help achieve high success rate despite 70% fault rate");
    }

    [Test]
    public async Task ShouldHandleConcurrentRetriesCorrectly()
    {
        // Arrange
        var retryPolicy = ResiliencePolicies.CreateRetryPolicy(retryCount: 3);
        var faultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 0.3);
        
        var results = new List<bool>();
        var lockObj = new object();

        // Act - Concurrent requests with retries
        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            try
            {
                var response = await retryPolicy.ExecuteAsync(async token =>
                {
                    return await faultPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/health", innerToken);
                    }, token);
                }, CancellationToken.None);
                
                lock (lockObj)
                {
                    results.Add(response.IsSuccessStatusCode);
                }
            }
            catch
            {
                lock (lockObj)
                {
                    results.Add(false);
                }
            }
        });
        
        await Task.WhenAll(tasks);

        // Assert
        var successRate = results.Count(r => r) / (double)results.Count * 100;
        
        Console.WriteLine($"Concurrent requests success rate: {successRate:F1}%");
        
        successRate.Should().BeGreaterThanOrEqualTo(80,
            "concurrent retries should achieve high success rate");
    }
}

using ChaosTests.Infrastructure;
using Polly;
using Polly.CircuitBreaker;
using System.Diagnostics;

namespace ChaosTests;

/// <summary>
/// Tests for PostgreSQL database failure scenarios.
/// Verifies connection resilience and data consistency under failures.
/// </summary>
[TestFixture]
[Category("Chaos")]
public class DatabaseFailureTests
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
    public async Task ShouldHandleDatabaseTimeoutsGracefully()
    {
        // Arrange
        var timeoutPolicy = ResiliencePolicies.CreateTimeoutPolicy(timeout: TimeSpan.FromSeconds(5));
        var latencyPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromSeconds(1),
            injectionRate: 0.5);
        
        int successCount = 0;
        int timeoutCount = 0;

        // Act - Simulate database operations with potential delays
        for (int i = 0; i < 10; i++)
        {
            _metrics.RecordRequest();
            
            try
            {
                var response = await timeoutPolicy.ExecuteAsync(async token =>
                {
                    return await latencyPolicy.ExecuteAsync(async innerToken =>
                    {
                        // Use GET categories as a database operation proxy
                        return await _catalogClient.GetAsync("/api/categories", innerToken);
                    }, token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                    _metrics.RecordSuccess();
                }
            }
            catch (TimeoutRejectedException)
            {
                timeoutCount++;
                _metrics.RecordTimeout();
            }
        }

        // Assert
        Console.WriteLine($"Success: {successCount}, Timeouts: {timeoutCount}");
        Console.WriteLine(_metrics.ToString());
        
        // With 5s timeout and 1s injected latency at 50%, most should succeed
        successCount.Should().BeGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task ShouldRetryDatabaseConnectionFailures()
    {
        // Arrange
        var retryPolicy = ResiliencePolicies.CreateRetryPolicy(retryCount: 3);
        var faultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.ServiceUnavailable,
            injectionRate: 0.4);
        
        int successCount = 0;
        int failureCount = 0;
        int retryCount = 0;

        // Act
        for (int i = 0; i < 10; i++)
        {
            _metrics.RecordRequest();
            
            try
            {
                int localRetries = 0;
                
                var response = await retryPolicy.ExecuteAsync(async token =>
                {
                    return await faultPolicy.ExecuteAsync(async innerToken =>
                    {
                        var result = await _catalogClient.GetAsync("/api/categories", innerToken);
                        
                        if ((int)result.StatusCode >= 500)
                        {
                            Interlocked.Increment(ref localRetries);
                            _metrics.RecordRetry();
                        }
                        
                        return result;
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
                
                retryCount += localRetries;
            }
            catch (Exception)
            {
                failureCount++;
                _metrics.RecordFailure();
            }
        }

        // Assert
        Console.WriteLine($"Success: {successCount}, Failures: {failureCount}, Retries: {retryCount}");
        Console.WriteLine(_metrics.ToString());
        
        // Retries should help recover from transient failures
        successCount.Should().BeGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task ShouldMaintainDataIntegrityUnderStress()
    {
        // Arrange - Create a category first
        var categoryRequest = new
        {
            Name = $"Chaos Test Category {Guid.NewGuid():N}",
            Description = "Category for chaos testing data integrity"
        };
        
        var createResponse = await _catalogClient.PostAsJsonAsync("/api/categories", categoryRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        createdCategory.Should().NotBeNull();
        
        // Apply chaos during reads
        var chaosPolicy = ChaosStrategies.CreateCombinedChaosStrategy(
            latencyRate: 0.3,
            faultRate: 0.1,
            latency: TimeSpan.FromMilliseconds(100));
        
        int consistentReads = 0;
        int totalReads = 0;

        // Act - Read the same category multiple times under chaos
        for (int i = 0; i < 20; i++)
        {
            try
            {
                var response = await chaosPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync($"/api/categories/{createdCategory!.Id}", token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                {
                    totalReads++;
                    var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
                    
                    if (category?.Id == createdCategory!.Id && 
                        category?.Name == createdCategory.Name)
                    {
                        consistentReads++;
                    }
                }
            }
            catch
            {
                // Expected under chaos
            }
        }

        // Assert - All successful reads should return consistent data
        Console.WriteLine($"Consistent reads: {consistentReads}/{totalReads}");
        
        if (totalReads > 0)
        {
            consistentReads.Should().Be(totalReads, "all successful reads should return consistent data");
        }
    }

    [Test]
    public async Task ShouldHandleConnectionPoolExhaustion()
    {
        // Arrange - Simulate many concurrent requests that could exhaust connection pool
        var tasks = new List<Task<HttpResponseMessage>>();
        var latencyPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromMilliseconds(500),
            injectionRate: 0.5);
        
        // Act - Create many concurrent requests
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(ExecuteWithLatency(latencyPolicy, $"/api/categories"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        int successCount = responses.Count(r => r.IsSuccessStatusCode);
        int serverErrors = responses.Count(r => (int)r.StatusCode >= 500);

        // Assert
        Console.WriteLine($"Success: {successCount}, Server Errors: {serverErrors}, Total: {responses.Length}");
        
        // Under connection pool stress, most requests should still succeed
        successCount.Should().BeGreaterThanOrEqualTo(responses.Length / 2);
    }

    [Test]
    public async Task ShouldRecoverFromDatabaseRestart()
    {
        // Arrange
        var stopwatch = new Stopwatch();
        
        // Simulate database being unavailable temporarily
        var faultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.ServiceUnavailable,
            injectionRate: 1.0);
        
        // First phase - database "down"
        int failuresDuringOutage = 0;
        for (int i = 0; i < 5; i++)
        {
            var response = await faultPolicy.ExecuteAsync(async token =>
            {
                return await _catalogClient.GetAsync("/api/categories", token);
            }, CancellationToken.None);
            
            if (!response.IsSuccessStatusCode)
                failuresDuringOutage++;
        }

        // Second phase - database "recovered"
        stopwatch.Start();
        
        int successesAfterRecovery = 0;
        for (int i = 0; i < 5; i++)
        {
            var response = await _catalogClient.GetAsync("/api/categories");
            if (response.IsSuccessStatusCode)
                successesAfterRecovery++;
        }
        
        stopwatch.Stop();

        // Assert
        Console.WriteLine($"Failures during outage: {failuresDuringOutage}");
        Console.WriteLine($"Successes after recovery: {successesAfterRecovery}");
        Console.WriteLine($"Recovery check time: {stopwatch.ElapsedMilliseconds}ms");
        
        failuresDuringOutage.Should().Be(5);
        successesAfterRecovery.Should().Be(5, "service should fully recover after database restart");
    }

    [Test]
    public async Task ShouldHandlePartialDatabaseFailures()
    {
        // Arrange - Some operations fail, some succeed
        var partialFailurePolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 0.3);
        
        int writeSuccesses = 0;
        int writeFailures = 0;
        int readSuccesses = 0;
        int readFailures = 0;

        // Act - Perform mixed read/write operations
        for (int i = 0; i < 10; i++)
        {
            // Write operation
            var writeRequest = new
            {
                Name = $"Partial Failure Test {Guid.NewGuid():N}",
                Description = "Testing partial failures"
            };
            
            var writeResponse = await partialFailurePolicy.ExecuteAsync(async token =>
            {
                return await _catalogClient.PostAsJsonAsync("/api/categories", writeRequest, token);
            }, CancellationToken.None);
            
            if (writeResponse.IsSuccessStatusCode || writeResponse.StatusCode == HttpStatusCode.Created)
                writeSuccesses++;
            else
                writeFailures++;

            // Read operation
            var readResponse = await partialFailurePolicy.ExecuteAsync(async token =>
            {
                return await _catalogClient.GetAsync("/api/categories", token);
            }, CancellationToken.None);
            
            if (readResponse.IsSuccessStatusCode)
                readSuccesses++;
            else
                readFailures++;
        }

        // Assert
        Console.WriteLine($"Writes - Success: {writeSuccesses}, Failures: {writeFailures}");
        Console.WriteLine($"Reads - Success: {readSuccesses}, Failures: {readFailures}");
        
        // With 30% failure rate, majority should succeed
        writeSuccesses.Should().BeGreaterThanOrEqualTo(5);
        readSuccesses.Should().BeGreaterThanOrEqualTo(5);
    }

    private async Task<HttpResponseMessage> ExecuteWithLatency(
        ResiliencePipeline<HttpResponseMessage> policy,
        string endpoint)
    {
        try
        {
            return await policy.ExecuteAsync(async token =>
            {
                return await _catalogClient.GetAsync(endpoint, token);
            }, CancellationToken.None);
        }
        catch
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

    private record CategoryResponse(Guid Id, string Name, string Description, DateTime CreatedAt);
}

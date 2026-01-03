using ChaosTests.Infrastructure;
using Polly;
using Polly.CircuitBreaker;
using System.Diagnostics;

namespace ChaosTests;

/// <summary>
/// Tests for circuit breaker behavior under failure conditions.
/// Verifies circuit breaker opens/closes correctly.
/// </summary>
[TestFixture]
[Category("Chaos")]
public class CircuitBreakerTests
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
    public async Task ShouldOpenCircuitAfterConsecutiveFailures()
    {
        // Arrange
        var circuitBreakerOpen = false;
        var circuitBreaker = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(5),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnOpened = args =>
                {
                    circuitBreakerOpen = true;
                    _metrics.RecordCircuitBreakerOpening();
                    Console.WriteLine($"Circuit opened at {DateTime.UtcNow:HH:mm:ss.fff}");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    circuitBreakerOpen = false;
                    Console.WriteLine($"Circuit closed at {DateTime.UtcNow:HH:mm:ss.fff}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
        
        // Fault policy that injects 500 errors
        var faultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 0.8);
        
        int requestCount = 0;
        int failureCount = 0;
        int rejectedCount = 0;

        // Act - Make requests until circuit opens
        for (int i = 0; i < 20; i++)
        {
            requestCount++;
            _metrics.RecordRequest();
            
            try
            {
                var response = await circuitBreaker.ExecuteAsync(async token =>
                {
                    return await faultPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/health", innerToken);
                    }, token);
                }, CancellationToken.None);
                
                if (response.IsSuccessStatusCode)
                    _metrics.RecordSuccess();
                else
                {
                    failureCount++;
                    _metrics.RecordFailure();
                }
            }
            catch (BrokenCircuitException)
            {
                rejectedCount++;
                Console.WriteLine($"Request {i + 1} rejected by circuit breaker");
            }
            
            // Short delay between requests
            await Task.Delay(50);
        }

        // Assert
        Console.WriteLine($"Requests: {requestCount}, Failures: {failureCount}, Rejected: {rejectedCount}");
        Console.WriteLine($"Circuit opened: {circuitBreakerOpen}");
        Console.WriteLine(_metrics.ToString());
        
        // With 80% failure rate and 50% threshold, circuit should open
        _metrics.CircuitBreakerOpenings.Should().BeGreaterThan(0, "circuit should have opened");
        rejectedCount.Should().BeGreaterThan(0, "some requests should be rejected when circuit is open");
    }

    [Test]
    public async Task ShouldAllowHalfOpenProbeAfterBreakDuration()
    {
        // Arrange
        var halfOpenReached = false;
        var breakDuration = TimeSpan.FromSeconds(2);
        
        var circuitBreaker = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(5),
                MinimumThroughput = 2,
                BreakDuration = breakDuration,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnHalfOpened = args =>
                {
                    halfOpenReached = true;
                    Console.WriteLine($"Circuit half-opened at {DateTime.UtcNow:HH:mm:ss.fff}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
        
        // First, trip the circuit with 100% failures
        var failPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 1.0);
        
        // Act - Trip the circuit
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async token =>
                {
                    return await failPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/health", innerToken);
                    }, token);
                }, CancellationToken.None);
            }
            catch (BrokenCircuitException)
            {
                Console.WriteLine("Circuit is open");
                break;
            }
        }
        
        // Wait for break duration
        Console.WriteLine($"Waiting {breakDuration.TotalSeconds}s for half-open state...");
        await Task.Delay(breakDuration + TimeSpan.FromMilliseconds(500));
        
        // Try a request that should be allowed in half-open state
        try
        {
            var response = await circuitBreaker.ExecuteAsync(async token =>
            {
                return await _catalogClient.GetAsync("/health", token);
            }, CancellationToken.None);
            
            Console.WriteLine($"Probe request result: {response.StatusCode}");
        }
        catch (BrokenCircuitException ex)
        {
            Console.WriteLine($"Probe rejected: {ex.Message}");
        }

        // Assert
        halfOpenReached.Should().BeTrue("circuit should reach half-open state after break duration");
    }

    [Test]
    public async Task ShouldCloseCircuitAfterSuccessfulProbe()
    {
        // Arrange
        var circuitClosed = false;
        var breakDuration = TimeSpan.FromSeconds(1);
        
        var circuitBreaker = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(5),
                MinimumThroughput = 2,
                BreakDuration = breakDuration,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnClosed = args =>
                {
                    circuitClosed = true;
                    Console.WriteLine($"Circuit closed at {DateTime.UtcNow:HH:mm:ss.fff}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
        
        // Trip the circuit with failures
        var failPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 1.0);
        
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async token =>
                {
                    return await failPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.GetAsync("/health", innerToken);
                    }, token);
                }, CancellationToken.None);
            }
            catch (BrokenCircuitException)
            {
                break;
            }
        }
        
        // Wait and then make successful requests
        await Task.Delay(breakDuration + TimeSpan.FromMilliseconds(500));
        
        // Act - Make successful requests to close circuit
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var response = await circuitBreaker.ExecuteAsync(async token =>
                {
                    return await _catalogClient.GetAsync("/health", token);
                }, CancellationToken.None);
                
                Console.WriteLine($"Request {i + 1}: {response.StatusCode}");
            }
            catch (BrokenCircuitException)
            {
                // May still be open
            }
            
            await Task.Delay(100);
        }

        // Assert
        circuitClosed.Should().BeTrue("circuit should close after successful probes");
    }

    [Test]
    public async Task ShouldNotTripCircuitForClientErrors()
    {
        // Arrange
        var circuitOpened = false;
        
        var circuitBreaker = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(5),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500), // Only 5xx errors
                OnOpened = args =>
                {
                    circuitOpened = true;
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // Act - Make requests that return 4xx errors (client errors)
        for (int i = 0; i < 10; i++)
        {
            var response = await circuitBreaker.ExecuteAsync(async token =>
            {
                // Request non-existent resource - should return 404
                return await _catalogClient.GetAsync($"/api/categories/{Guid.NewGuid()}", token);
            }, CancellationToken.None);
            
            Console.WriteLine($"Request {i + 1}: {response.StatusCode}");
        }

        // Assert
        circuitOpened.Should().BeFalse("circuit should not open for 4xx client errors");
    }

    [Test]
    public async Task ShouldTrackCircuitBreakerMetrics()
    {
        // Arrange
        int openCount = 0;
        int closeCount = 0;
        int halfOpenCount = 0;
        
        var circuitBreaker = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(3),
                MinimumThroughput = 2,
                BreakDuration = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnOpened = _ => { openCount++; return ValueTask.CompletedTask; },
                OnClosed = _ => { closeCount++; return ValueTask.CompletedTask; },
                OnHalfOpened = _ => { halfOpenCount++; return ValueTask.CompletedTask; }
            })
            .Build();
        
        var faultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.InternalServerError,
            injectionRate: 0.7);

        // Act - Make requests with varying failure rates
        for (int cycle = 0; cycle < 3; cycle++)
        {
            // Phase 1: Induce failures to open circuit
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(async token =>
                    {
                        return await faultPolicy.ExecuteAsync(async innerToken =>
                        {
                            return await _catalogClient.GetAsync("/health", innerToken);
                        }, token);
                    }, CancellationToken.None);
                }
                catch (BrokenCircuitException) { }
            }
            
            // Phase 2: Wait and recover
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            
            // Phase 3: Successful requests to close circuit
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(async token =>
                    {
                        return await _catalogClient.GetAsync("/health", token);
                    }, CancellationToken.None);
                }
                catch (BrokenCircuitException) { }
            }
        }

        // Assert
        Console.WriteLine($"Opens: {openCount}, Closes: {closeCount}, HalfOpens: {halfOpenCount}");
        
        openCount.Should().BeGreaterThan(0, "circuit should have opened during test");
    }
}

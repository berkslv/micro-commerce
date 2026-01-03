using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace ChaosTests.Infrastructure;

/// <summary>
/// Provides resilience policies for chaos testing scenarios.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates a retry policy with exponential backoff.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateRetryPolicy(
        int retryCount = 3,
        TimeSpan? initialDelay = null)
    {
        var delay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = retryCount,
                Delay = delay,
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            })
            .Build();
    }

    /// <summary>
    /// Creates a circuit breaker policy.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateCircuitBreakerPolicy(
        int failureThreshold = 3,
        TimeSpan? breakDuration = null)
    {
        var duration = breakDuration ?? TimeSpan.FromSeconds(30);
        
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = failureThreshold,
                BreakDuration = duration,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            })
            .Build();
    }

    /// <summary>
    /// Creates a timeout policy.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateTimeoutPolicy(
        TimeSpan? timeout = null)
    {
        var timeoutDuration = timeout ?? TimeSpan.FromSeconds(5);
        
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeoutDuration
            })
            .Build();
    }

    /// <summary>
    /// Creates a combined resilience pipeline with retry, circuit breaker, and timeout.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateCombinedPolicy(
        int retryCount = 3,
        int circuitBreakerThreshold = 3,
        TimeSpan? timeout = null)
    {
        var timeoutDuration = timeout ?? TimeSpan.FromSeconds(10);
        
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeoutDuration
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = circuitBreakerThreshold,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = retryCount,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            })
            .Build();
    }
}

/// <summary>
/// Tracks metrics for resilience policy testing.
/// </summary>
public class ResilienceMetrics
{
    private int _totalRequests;
    private int _successfulRequests;
    private int _failedRequests;
    private int _retryAttempts;
    private int _circuitBreakerOpenings;
    private int _timeouts;

    public int TotalRequests => _totalRequests;
    public int SuccessfulRequests => _successfulRequests;
    public int FailedRequests => _failedRequests;
    public int RetryAttempts => _retryAttempts;
    public int CircuitBreakerOpenings => _circuitBreakerOpenings;
    public int Timeouts => _timeouts;

    public double SuccessRate => TotalRequests > 0 
        ? (double)SuccessfulRequests / TotalRequests * 100 
        : 0;

    public void RecordRequest() => Interlocked.Increment(ref _totalRequests);
    public void RecordSuccess() => Interlocked.Increment(ref _successfulRequests);
    public void RecordFailure() => Interlocked.Increment(ref _failedRequests);
    public void RecordRetry() => Interlocked.Increment(ref _retryAttempts);
    public void RecordCircuitBreakerOpening() => Interlocked.Increment(ref _circuitBreakerOpenings);
    public void RecordTimeout() => Interlocked.Increment(ref _timeouts);

    public void Reset()
    {
        _totalRequests = 0;
        _successfulRequests = 0;
        _failedRequests = 0;
        _retryAttempts = 0;
        _circuitBreakerOpenings = 0;
        _timeouts = 0;
    }

    public override string ToString()
    {
        return $"Total: {TotalRequests}, Success: {SuccessfulRequests} ({SuccessRate:F1}%), " +
               $"Failed: {FailedRequests}, Retries: {RetryAttempts}, " +
               $"Circuit Opens: {CircuitBreakerOpenings}, Timeouts: {Timeouts}";
    }
}

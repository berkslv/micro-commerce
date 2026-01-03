using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;

namespace ChaosTests.Infrastructure;

/// <summary>
/// Provides chaos injection strategies using Polly Simmy for fault injection testing.
/// </summary>
public static class ChaosStrategies
{
    /// <summary>
    /// Creates a strategy that injects latency into requests.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateLatencyStrategy(
        TimeSpan latency,
        double injectionRate = 0.5)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddChaosLatency(new ChaosLatencyStrategyOptions
            {
                Latency = latency,
                InjectionRate = injectionRate,
                Enabled = true
            })
            .Build();
    }

    /// <summary>
    /// Creates a strategy that injects exceptions into requests.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateFaultStrategy(
        Exception fault,
        double injectionRate = 0.5)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddChaosFault(new ChaosFaultStrategyOptions
            {
                FaultGenerator = new FaultGenerator().AddException(() => fault),
                InjectionRate = injectionRate,
                Enabled = true
            })
            .Build();
    }

    /// <summary>
    /// Creates a strategy that injects HTTP error responses.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateHttpErrorStrategy(
        HttpStatusCode statusCode,
        double injectionRate = 0.5)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
            {
                OutcomeGenerator = new OutcomeGenerator<HttpResponseMessage>()
                    .AddResult(() => new HttpResponseMessage(statusCode)),
                InjectionRate = injectionRate,
                Enabled = true
            })
            .Build();
    }

    /// <summary>
    /// Creates a strategy that simulates network failure.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateNetworkFailureStrategy(
        double injectionRate = 0.5)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddChaosFault(new ChaosFaultStrategyOptions
            {
                FaultGenerator = new FaultGenerator()
                    .AddException<HttpRequestException>(),
                InjectionRate = injectionRate,
                Enabled = true
            })
            .Build();
    }

    /// <summary>
    /// Creates a strategy that simulates timeout.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateTimeoutStrategy(
        double injectionRate = 0.5)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddChaosFault(new ChaosFaultStrategyOptions
            {
                FaultGenerator = new FaultGenerator()
                    .AddException<TaskCanceledException>(),
                InjectionRate = injectionRate,
                Enabled = true
            })
            .Build();
    }

    /// <summary>
    /// Creates a combined chaos strategy with multiple failure modes.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateCombinedChaosStrategy(
        double latencyRate = 0.3,
        double faultRate = 0.2,
        TimeSpan? latency = null)
    {
        var latencyDuration = latency ?? TimeSpan.FromSeconds(2);
        
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddChaosLatency(new ChaosLatencyStrategyOptions
            {
                Latency = latencyDuration,
                InjectionRate = latencyRate,
                Enabled = true
            })
            .AddChaosFault(new ChaosFaultStrategyOptions
            {
                FaultGenerator = new FaultGenerator()
                    .AddException<HttpRequestException>(),
                InjectionRate = faultRate,
                Enabled = true
            })
            .Build();
    }
}

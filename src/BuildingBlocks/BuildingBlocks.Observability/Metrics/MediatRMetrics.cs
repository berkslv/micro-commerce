using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BuildingBlocks.Observability.Metrics;

/// <summary>
/// Custom metrics for MediatR request tracking
/// </summary>
public sealed class MediatRMetrics : IDisposable
{
    public const string MeterName = "MediatR";

    private readonly Meter _meter;
    private readonly Counter<long> _requestsCounter;
    private readonly Counter<long> _requestErrorsCounter;
    private readonly Histogram<double> _requestDuration;

    public MediatRMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _requestsCounter = _meter.CreateCounter<long>(
            name: "mediatr.requests",
            unit: "{request}",
            description: "Total number of MediatR requests");

        _requestErrorsCounter = _meter.CreateCounter<long>(
            name: "mediatr.requests.errors",
            unit: "{request}",
            description: "Total number of MediatR request errors");

        _requestDuration = _meter.CreateHistogram<double>(
            name: "mediatr.request.duration",
            unit: "ms",
            description: "Duration of MediatR requests in milliseconds");
    }

    public void RecordRequest(string requestName, bool success, double durationMs, string? exceptionType = null)
    {
        var tags = new TagList
        {
            { "request.name", requestName },
            { "request.success", success }
        };

        _requestsCounter.Add(1, tags);
        _requestDuration.Record(durationMs, tags);

        if (!success)
        {
            var errorTags = new TagList
            {
                { "request.name", requestName },
                { "exception.type", exceptionType ?? "Unknown" }
            };
            _requestErrorsCounter.Add(1, errorTags);
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}

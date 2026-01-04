using System.Diagnostics;
using BuildingBlocks.Observability.Metrics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Behaviors;

/// <summary>
/// Pipeline behavior for logging requests and responses with metrics and tracing.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new("MediatR");
    
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly MediatRMetrics _metrics;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        MediatRMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        
        using var activity = ActivitySource.StartActivity($"MediatR {requestName}", ActivityKind.Internal);
        activity?.SetTag("mediatr.request.name", requestName);
        activity?.SetTag("mediatr.request.type", typeof(TRequest).FullName);

        _logger.LogInformation(
            "Handling {RequestName} {@Request}",
            requestName,
            request);

        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            activity?.SetTag("mediatr.request.success", true);
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            _metrics.RecordRequest(requestName, success: true, stopwatch.Elapsed.TotalMilliseconds);

            _logger.LogInformation(
                "Handled {RequestName} with response {@Response} in {ElapsedMs}ms",
                requestName,
                response,
                stopwatch.Elapsed.TotalMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            activity?.SetTag("mediatr.request.success", false);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            
            _metrics.RecordRequest(requestName, success: false, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);

            _logger.LogError(ex,
                "Error handling {RequestName} after {ElapsedMs}ms",
                requestName,
                stopwatch.Elapsed.TotalMilliseconds);

            throw;
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Behaviors;

/// <summary>
/// Pipeline behavior for logging requests and responses.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "Handling {RequestName} {@Request}",
            requestName,
            request);

        var response = await next();

        _logger.LogInformation(
            "Handled {RequestName} with response {@Response}",
            requestName,
            response);

        return response;
    }
}

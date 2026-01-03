using BuildingBlocks.Messaging.Models;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Serilog.Events;

namespace BuildingBlocks.Messaging.Filters.Correlations;

/// <summary>
/// ASP.NET Core middleware that extracts or creates CorrelationId from HTTP headers.
/// </summary>
public class CorrelationMiddleware : IMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        AsyncStorage<Correlation>.Store(new Correlation { Id = correlationId });

        using (LogContext.PushProperty("CorrelationId", new ScalarValue(correlationId)))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId.ToString());
                return Task.CompletedTask;
            });

            await next(context);
        }
    }

    private static Guid GetOrCreateCorrelationId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationIdHeader))
        {
            return Guid.NewGuid();
        }

        if (Guid.TryParse(correlationIdHeader, out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid();
    }
}

using System.Text.Json;
using BuildingBlocks.Messaging.Models;
using MassTransit;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using LogContext = Serilog.Context.LogContext;

namespace BuildingBlocks.Messaging.Filters.Correlations;

/// <summary>
/// MassTransit filter that extracts CorrelationId from consumed messages.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class CorrelationConsumeFilter<T>(ILogger<CorrelationConsumeFilter<T>> logger)
    : IFilter<ConsumeContext<T>> where T : class
{
    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var correlationIdHeader = context.CorrelationId;

        if (correlationIdHeader.HasValue)
        {
            var correlationId = correlationIdHeader.Value;

            LogContext.PushProperty("CorrelationId", new ScalarValue(correlationId));

            AsyncStorage<Correlation>.Store(new Correlation
            {
                Id = correlationId
            });
        }

        logger.LogInformation(
            "Event {EventType} with content {Event} has been consumed",
            context.Message.GetType().Name,
            JsonSerializer.Serialize(context.Message));

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

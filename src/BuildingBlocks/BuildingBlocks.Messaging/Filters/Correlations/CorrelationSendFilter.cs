using System.Text.Json;
using BuildingBlocks.Messaging.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging.Filters.Correlations;

/// <summary>
/// MassTransit filter that adds CorrelationId to sent messages.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class CorrelationSendFilter<T>(ILogger<CorrelationSendFilter<T>> logger)
    : IFilter<SendContext<T>> where T : class
{
    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        var correlation = AsyncStorage<Correlation>.Retrieve();

        if (correlation is not null)
        {
            context.CorrelationId = correlation.Id;
        }

        logger.LogInformation(
            "Event {EventType} with content {Event} has been sent",
            context.Message.GetType().Name,
            JsonSerializer.Serialize(context.Message));

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

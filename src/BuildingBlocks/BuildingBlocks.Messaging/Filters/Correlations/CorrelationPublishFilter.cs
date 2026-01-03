using System.Text.Json;
using BuildingBlocks.Messaging.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging.Filters.Correlations;

/// <summary>
/// MassTransit filter that adds CorrelationId to published messages.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class CorrelationPublishFilter<T>(ILogger<CorrelationPublishFilter<T>> logger)
    : IFilter<PublishContext<T>> where T : class
{
    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var correlation = AsyncStorage<Correlation>.Retrieve();

        if (correlation is not null)
        {
            context.CorrelationId = correlation.Id;
        }

        logger.LogInformation(
            "Event {EventType} with content {Event} has been published",
            context.Message.GetType().Name,
            JsonSerializer.Serialize(context.Message));

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

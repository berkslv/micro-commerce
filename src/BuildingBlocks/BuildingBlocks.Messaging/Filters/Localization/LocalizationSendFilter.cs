using System.Globalization;
using MassTransit;

namespace BuildingBlocks.Messaging.Filters.Localization;

/// <summary>
/// MassTransit filter that adds current culture to sent message headers.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class LocalizationSendFilter<T> : IFilter<SendContext<T>> where T : class
{
    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        var currentCulture = CultureInfo.CurrentCulture.Name;
        context.Headers.Set("Accept-Language", currentCulture);

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

using System.Globalization;
using MassTransit;

namespace BuildingBlocks.Messaging.Filters.Localization;

/// <summary>
/// MassTransit filter that adds current culture to published message headers.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class LocalizationPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var currentCulture = CultureInfo.CurrentCulture.Name;
        context.Headers.Set("Accept-Language", currentCulture);

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

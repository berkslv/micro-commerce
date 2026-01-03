using System.Globalization;
using MassTransit;

namespace BuildingBlocks.Messaging.Filters.Localization;

/// <summary>
/// MassTransit filter that sets culture from consumed message headers.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class LocalizationConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var cultureKey = context.Headers.Get<string>("Accept-Language");

        CultureInfo culture;

        if (!string.IsNullOrEmpty(cultureKey) && LocalizationExtensions.IsCultureAcceptable(cultureKey))
        {
            culture = new CultureInfo(cultureKey);
        }
        else
        {
            culture = new CultureInfo("en-US");
        }

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

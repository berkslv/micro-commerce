using BuildingBlocks.Messaging.Models;
using MassTransit;

namespace BuildingBlocks.Messaging.Filters.Tokens;

/// <summary>
/// MassTransit filter that adds Token to published message headers.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class TokenPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var token = AsyncStorage<Token>.Retrieve();

        if (token is not null && !string.IsNullOrEmpty(token.Content))
        {
            context.Headers.Set("Token", token.Content);
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

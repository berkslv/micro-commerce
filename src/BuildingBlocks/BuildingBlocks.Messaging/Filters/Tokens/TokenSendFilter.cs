using BuildingBlocks.Messaging.Models;
using MassTransit;

namespace BuildingBlocks.Messaging.Filters.Tokens;

/// <summary>
/// MassTransit filter that adds Token to sent message headers.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class TokenSendFilter<T> : IFilter<SendContext<T>> where T : class
{
    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
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

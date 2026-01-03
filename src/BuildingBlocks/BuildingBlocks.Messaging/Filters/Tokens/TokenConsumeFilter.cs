using BuildingBlocks.Messaging.Models;
using MassTransit;

namespace BuildingBlocks.Messaging.Filters.Tokens;

/// <summary>
/// MassTransit filter that extracts Token from consumed message headers.
/// </summary>
/// <typeparam name="T">Event type</typeparam>
public class TokenConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var tokenContent = context.Headers.Get<string>("Token");

        if (tokenContent is not null)
        {
            AsyncStorage<Token>.Store(new Token
            {
                Content = tokenContent
            });
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

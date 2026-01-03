using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Products.Commands.SyncProductUpdated;
using MassTransit;
using MediatR;

namespace Order.API.Consumers;

/// <summary>
/// Consumer for ProductUpdatedEvent to sync product updates to Order service read model.
/// </summary>
public sealed class ProductUpdatedConsumer : IConsumer<ProductUpdatedEvent>
{
    private readonly ISender _sender;

    public ProductUpdatedConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var message = context.Message;
        await _sender.Send(new SyncProductUpdatedCommand(
            message.ProductId,
            message.Name,
            message.Price,
            message.Currency,
            message.IsAvailable));
    }
}

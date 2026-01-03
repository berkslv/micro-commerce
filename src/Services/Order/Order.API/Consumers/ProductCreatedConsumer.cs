using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Products.Commands.SyncProductCreated;
using MassTransit;
using MediatR;

namespace Order.API.Consumers;

/// <summary>
/// Consumer for ProductCreatedEvent to sync product data to Order service read model.
/// </summary>
public sealed class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly ISender _sender;

    public ProductCreatedConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;
        await _sender.Send(new SyncProductCreatedCommand(
            message.ProductId,
            message.Name,
            message.Price,
            message.Currency,
            message.IsAvailable));
    }
}

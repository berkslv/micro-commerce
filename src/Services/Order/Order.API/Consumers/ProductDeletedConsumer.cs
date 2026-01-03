using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Products.Commands.SyncProductDeleted;
using MassTransit;
using MediatR;

namespace Order.API.Consumers;

/// <summary>
/// Consumer for ProductDeletedEvent to mark product as unavailable in Order service read model.
/// </summary>
public sealed class ProductDeletedConsumer : IConsumer<ProductDeletedEvent>
{
    private readonly ISender _sender;

    public ProductDeletedConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
    {
        var message = context.Message;
        await _sender.Send(new SyncProductDeletedCommand(message.ProductId));
    }
}

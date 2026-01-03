using BuildingBlocks.Messaging.Events;
using Catalog.Application.Features.Stock.Commands.ProcessOrderCancelled;
using MassTransit;
using MediatR;

namespace Catalog.API.Consumers;

/// <summary>
/// Consumer for OrderCancelledEvent - delegates to MediatR handler.
/// </summary>
public sealed class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
    private readonly ISender _sender;

    public OrderCancelledConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var message = context.Message;
        await _sender.Send(new ProcessOrderCancelledCommand(
            message.OrderId,
            message.Items.Select(i => new CancelledItemData(i.ProductId, i.Quantity)).ToList()));
    }
}

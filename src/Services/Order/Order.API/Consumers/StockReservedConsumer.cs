using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Orders.Commands.MarkStockReserved;
using MassTransit;
using MediatR;

namespace Order.API.Consumers;

/// <summary>
/// Consumer for StockReservedEvent to update order status and confirm order.
/// </summary>
public sealed class StockReservedConsumer : IConsumer<StockReservedEvent>
{
    private readonly ISender _sender;

    public StockReservedConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var message = context.Message;
        await _sender.Send(new MarkStockReservedCommand(message.OrderId));
    }
}

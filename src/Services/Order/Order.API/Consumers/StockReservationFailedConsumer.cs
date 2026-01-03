using BuildingBlocks.Messaging.Events;
using Order.Application.Features.Orders.Commands.MarkStockReservationFailed;
using MassTransit;
using MediatR;

namespace Order.API.Consumers;

/// <summary>
/// Consumer for StockReservationFailedEvent to cancel the order.
/// </summary>
public sealed class StockReservationFailedConsumer : IConsumer<StockReservationFailedEvent>
{
    private readonly ISender _sender;

    public StockReservationFailedConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<StockReservationFailedEvent> context)
    {
        var message = context.Message;
        await _sender.Send(new MarkStockReservationFailedCommand(
            message.OrderId,
            message.Reason));
    }
}

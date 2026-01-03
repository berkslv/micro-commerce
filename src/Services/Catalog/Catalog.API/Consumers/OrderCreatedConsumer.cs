using BuildingBlocks.Messaging.Events;
using Catalog.Application.Features.Stock.Commands.ProcessOrderCreated;
using MassTransit;
using MediatR;

namespace Catalog.API.Consumers;

/// <summary>
/// Consumer for OrderCreatedEvent - delegates to MediatR handler.
/// </summary>
public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ISender _sender;

    public OrderCreatedConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        await _sender.Send(new ProcessOrderCreatedCommand(
            message.OrderId,
            message.CorrelationId,
            message.Items));
    }
}

namespace Order.Application.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Response for CancelOrderCommand.
/// </summary>
public sealed record CancelOrderResponse(
    Guid OrderId,
    string Status,
    string Reason);

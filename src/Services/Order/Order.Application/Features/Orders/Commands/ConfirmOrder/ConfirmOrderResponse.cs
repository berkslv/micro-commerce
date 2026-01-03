namespace Order.Application.Features.Orders.Commands.ConfirmOrder;

/// <summary>
/// Response for ConfirmOrderCommand.
/// </summary>
public sealed record ConfirmOrderResponse(
    Guid OrderId,
    string Status,
    decimal TotalAmount,
    string Currency);

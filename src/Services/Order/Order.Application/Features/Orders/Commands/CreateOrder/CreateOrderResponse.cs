namespace Order.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Response for CreateOrderCommand.
/// </summary>
public sealed record CreateOrderResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt);

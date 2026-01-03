namespace Order.Application.Features.Orders.Queries.GetOrder;

/// <summary>
/// Response for GetOrderQuery.
/// </summary>
public sealed record GetOrderResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    string ShippingAddress,
    string Status,
    decimal TotalAmount,
    string Currency,
    string? Notes,
    IReadOnlyList<OrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

/// <summary>
/// Order item response with joined product data.
/// </summary>
public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal TotalPrice,
    decimal? CurrentProductPrice,
    bool IsProductAvailable);

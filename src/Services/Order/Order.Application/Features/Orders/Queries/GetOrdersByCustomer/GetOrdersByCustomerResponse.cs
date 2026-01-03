namespace Order.Application.Features.Orders.Queries.GetOrdersByCustomer;

/// <summary>
/// Response for GetOrdersByCustomerQuery.
/// </summary>
public sealed record GetOrdersByCustomerResponse(
    Guid Id,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt);

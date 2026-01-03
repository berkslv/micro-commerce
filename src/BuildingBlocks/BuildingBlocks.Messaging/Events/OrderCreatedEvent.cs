namespace BuildingBlocks.Messaging.Events;

/// <summary>
/// Event published when a new order is created.
/// </summary>
public record OrderCreatedEvent(
    Guid OrderId,
    DateTime OccurredAt,
    string CorrelationId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<OrderItemData> Items);

/// <summary>
/// Data for order item.
/// </summary>
public record OrderItemData(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity);

namespace BuildingBlocks.Messaging.Events;

/// <summary>
/// Event published when an order is cancelled.
/// </summary>
public record OrderCancelledEvent(
    Guid OrderId,
    DateTime OccurredAt,
    string CorrelationId,
    Guid CustomerId,
    string Reason,
    IReadOnlyList<CancelledOrderItemData> Items);

/// <summary>
/// Data for cancelled order item.
/// </summary>
public record CancelledOrderItemData(Guid ProductId, int Quantity);

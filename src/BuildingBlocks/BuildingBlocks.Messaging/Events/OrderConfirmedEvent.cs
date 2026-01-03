namespace BuildingBlocks.Messaging.Events;

/// <summary>
/// Event published when an order is confirmed.
/// </summary>
public record OrderConfirmedEvent(
    Guid OrderId,
    DateTime OccurredAt,
    string CorrelationId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency);

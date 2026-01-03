namespace BuildingBlocks.Messaging.Events;

/// <summary>
/// Event published when stock reservation fails.
/// </summary>
public record StockReservationFailedEvent(
    Guid OrderId,
    DateTime OccurredAt,
    string CorrelationId,
    string Reason);

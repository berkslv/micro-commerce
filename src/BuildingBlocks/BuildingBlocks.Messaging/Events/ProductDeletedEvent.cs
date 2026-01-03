namespace BuildingBlocks.Messaging.Events;

/// <summary>
/// Event published when a product is deleted.
/// </summary>
public record ProductDeletedEvent(
    Guid ProductId,
    DateTime OccurredAt,
    string CorrelationId);

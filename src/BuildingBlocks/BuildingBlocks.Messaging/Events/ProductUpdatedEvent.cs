namespace BuildingBlocks.Messaging.Events;

/// <summary>
/// Event published when a product is updated.
/// </summary>
public record ProductUpdatedEvent(
    Guid ProductId,
    DateTime OccurredAt,
    string CorrelationId,
    string Name,
    decimal Price,
    string Currency,
    int StockQuantity,
    Guid CategoryId,
    bool IsAvailable);

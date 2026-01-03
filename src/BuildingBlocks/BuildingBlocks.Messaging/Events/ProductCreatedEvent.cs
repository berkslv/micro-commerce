namespace BuildingBlocks.Messaging.Events;

/// <summary>
/// Event published when a new product is created.
/// </summary>
public record ProductCreatedEvent(
    Guid ProductId,
    DateTime OccurredAt,
    string CorrelationId,
    string Name,
    decimal Price,
    string Currency,
    int StockQuantity,
    Guid CategoryId,
    bool IsAvailable);

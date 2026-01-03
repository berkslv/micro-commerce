namespace BuildingBlocks.Messaging.Events;

/// <summary>
/// Event published when stock is successfully reserved for an order.
/// </summary>
public record StockReservedEvent(
    Guid OrderId,
    DateTime OccurredAt,
    string CorrelationId,
    IReadOnlyList<ReservedProductData> Products);

/// <summary>
/// Data for reserved product.
/// </summary>
public record ReservedProductData(Guid ProductId, int QuantityReserved);

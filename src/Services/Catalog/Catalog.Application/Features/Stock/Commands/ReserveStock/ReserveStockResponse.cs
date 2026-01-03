namespace Catalog.Application.Features.Stock.Commands.ReserveStock;

/// <summary>
/// Response for ReserveStockCommand.
/// </summary>
public sealed record ReserveStockResponse(
    Guid ProductId,
    Guid OrderId,
    int RequestedQuantity,
    bool Success,
    int RemainingStock);

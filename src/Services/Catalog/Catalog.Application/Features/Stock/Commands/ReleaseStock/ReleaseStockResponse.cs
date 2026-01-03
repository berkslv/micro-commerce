namespace Catalog.Application.Features.Stock.Commands.ReleaseStock;

/// <summary>
/// Response model for ReleaseStockCommand.
/// </summary>
public sealed record ReleaseStockResponse(
    Guid ProductId,
    Guid OrderId,
    int QuantityReleased,
    int CurrentStock);

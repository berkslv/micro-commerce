namespace Order.Application.Features.Orders.Commands.MarkStockReserved;

/// <summary>
/// Response model for MarkStockReservedCommand.
/// </summary>
public sealed record MarkStockReservedResponse(
    Guid OrderId,
    string Status);

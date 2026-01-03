namespace Order.Application.Features.Orders.Commands.MarkStockReservationFailed;

/// <summary>
/// Response model for MarkStockReservationFailedCommand.
/// </summary>
public sealed record MarkStockReservationFailedResponse(
    Guid OrderId,
    string Status,
    string FailureReason);

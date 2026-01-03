namespace Order.Domain.Enums;

/// <summary>
/// Represents the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order has been created but not yet processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Stock has been reserved for the order.
    /// </summary>
    StockReserved = 1,

    /// <summary>
    /// Order has been confirmed.
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Order is being processed for shipping.
    /// </summary>
    Processing = 3,

    /// <summary>
    /// Order has been shipped.
    /// </summary>
    Shipped = 4,

    /// <summary>
    /// Order has been delivered.
    /// </summary>
    Delivered = 5,

    /// <summary>
    /// Order has been cancelled.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Stock reservation failed.
    /// </summary>
    StockReservationFailed = 7
}

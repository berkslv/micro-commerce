using BuildingBlocks.Common.Domain;
using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

/// <summary>
/// Represents an order aggregate root.
/// </summary>
public sealed class Order : BaseAuditableEntity, IAggregateRoot
{
    private readonly List<object> _domainEvents = new();

    private readonly List<OrderItem> _items = new();

    public IReadOnlyList<object> DomainEvents
    {
        get
        {
            return _domainEvents.AsReadOnly();
        }
    }

    public IReadOnlyList<OrderItem> Items
    {
        get
        {
            return _items.AsReadOnly();
        }
    }

    public Guid CustomerId { get; }

    public string CustomerEmail { get; }

    public Address ShippingAddress { get; }

    public OrderStatus Status { get; private set; }

    public Money TotalAmount { get; private set; }

    public string? Notes { get; private set; }

    private Order(
        Guid customerId,
        string customerEmail,
        Address shippingAddress,
        string? notes)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        CustomerEmail = customerEmail;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        TotalAmount = Money.Zero();
        Notes = notes;
    }

    public static Order Create(
        Guid customerId,
        string customerEmail,
        Address shippingAddress,
        string? notes = null)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer ID is required.");
        }

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            throw new DomainException("Customer email is required.");
        }

        return new Order(customerId, customerEmail, shippingAddress, notes);
    }

    public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem is not null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var item = OrderItem.Create(Id, productId, productName, unitPrice, quantity);
            _items.Add(item);
        }

        RecalculateTotal();
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);

        if (item is null)
        {
            throw new DomainException($"Item with product ID {productId} not found in order.");
        }

        _items.Remove(item);
        RecalculateTotal();
    }

    public void Submit(string correlationId)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Only pending orders can be submitted.");
        }

        if (_items.Count == 0)
        {
            throw new DomainException("Cannot submit an order without items.");
        }

        // Create OrderCreatedEvent to trigger stock reservation
        var orderItems = _items.ConvertAll(i => new OrderItemData(
            i.ProductId,
            i.ProductName,
            i.UnitPrice.Amount,
            i.UnitPrice.Currency,
            i.Quantity))
;

        _domainEvents.Add(new OrderCreatedEvent(
            Id,
            DateTime.UtcNow,
            correlationId,
            CustomerId,
            TotalAmount.Amount,
            TotalAmount.Currency,
            orderItems));
    }

    public void MarkStockReserved()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Only pending orders can have stock reserved.");
        }

        Status = OrderStatus.StockReserved;
    }

    public void MarkStockReservationFailed(string reason)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Only pending orders can have stock reservation fail.");
        }

        Status = OrderStatus.StockReservationFailed;
        Notes = $"Stock reservation failed: {reason}";
    }

    public void Confirm(string correlationId)
    {
        if (Status != OrderStatus.StockReserved)
            throw new DomainException("Only orders with reserved stock can be confirmed.");

        Status = OrderStatus.Confirmed;

        _domainEvents.Add(new OrderConfirmedEvent(
            Id,
            DateTime.UtcNow,
            correlationId,
            CustomerId,
            TotalAmount.Amount,
            TotalAmount.Currency));
    }

    public void Cancel(string reason, string correlationId)
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException("Order is already cancelled.");
        }

        if (Status == OrderStatus.Delivered)
        {
            throw new DomainException("Delivered orders cannot be cancelled.");
        }

        var previousStatus = Status;
        Status = OrderStatus.Cancelled;
        Notes = $"Cancelled: {reason}";

        // Only publish event if stock was reserved (to restore it)
        if (previousStatus == OrderStatus.StockReserved || previousStatus == OrderStatus.Confirmed)
        {
            var cancelledItems = _items.ConvertAll(i => new CancelledOrderItemData(
                i.ProductId,
                i.Quantity))
;

            _domainEvents.Add(new OrderCancelledEvent(
                Id,
                DateTime.UtcNow,
                correlationId,
                CustomerId,
                reason,
                cancelledItems));
        }
    }

    public void MarkAsProcessing()
    {
        if (Status != OrderStatus.Confirmed)
        {
            throw new DomainException("Only confirmed orders can be processed.");
        }

        Status = OrderStatus.Processing;
    }

    public void MarkAsShipped()
    {
        if (Status != OrderStatus.Processing)
        {
            throw new DomainException("Only processing orders can be shipped.");
        }

        Status = OrderStatus.Shipped;
    }

    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
        {
            throw new DomainException("Only shipped orders can be delivered.");
        }

        Status = OrderStatus.Delivered;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void RecalculateTotal()
    {
        if (_items.Count == 0)
        {
            TotalAmount = Money.Zero();
            return;
        }

        var currency = _items[0].UnitPrice.Currency;
        var total = _items.Sum(i => i.TotalPrice.Amount);
        TotalAmount = Money.Create(total, currency);
    }

    /// <summary>
    /// EF Core parameterless constructor
    /// </summary>
    private Order()
    {
        CustomerEmail = string.Empty;
        ShippingAddress = null!;
        TotalAmount = Money.Zero();
    }
}

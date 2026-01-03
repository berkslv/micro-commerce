using BuildingBlocks.Common.Domain;
using BuildingBlocks.Common.Exceptions;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

/// <summary>
/// Represents an order item entity.
/// </summary>
public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Money TotalPrice => UnitPrice.Multiply(Quantity);

    private OrderItem(
        Guid orderId,
        Guid productId,
        string productName,
        Money unitPrice,
        int quantity)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public static OrderItem Create(
        Guid orderId,
        Guid productId,
        string productName,
        Money unitPrice,
        int quantity)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product ID is required.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        return new OrderItem(orderId, productId, productName, unitPrice, quantity);
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        Quantity = quantity;
    }

    // EF Core parameterless constructor
    private OrderItem()
    {
        ProductName = string.Empty;
        UnitPrice = Money.Zero();
    }
}

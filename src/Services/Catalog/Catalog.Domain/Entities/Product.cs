using BuildingBlocks.Common.Domain;
using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Entities;

/// <summary>
/// Product aggregate root with domain logic for stock management.
/// </summary>
public sealed class Product : BaseAuditableEntity, IAggregateRoot
{
    public ProductName Name { get; private set; } = null!;

    public string Description { get; private set; } = string.Empty;

    public Money Price { get; private set; } = null!;

    public int StockQuantity { get; private set; }

    public Sku Sku { get; private set; } = null!;

    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Navigation
    /// </summary>
    public Category Category { get; } = null!;

    private readonly List<object> _domainEvents = new();

    public IReadOnlyList<object> DomainEvents
    {
        get
        {
            return _domainEvents.AsReadOnly();
        }
    }

    private Product()
    {
    }

    public static Product Create(
        ProductName name,
        string description,
        Money price,
        int stockQuantity,
        Sku sku,
        Guid categoryId)
    {
        if (stockQuantity < 0)
        {
            throw new DomainException("Stock quantity cannot be negative");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description ?? string.Empty,
            Price = price,
            StockQuantity = stockQuantity,
            Sku = sku,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };

        product._domainEvents.Add(new ProductCreatedEvent(
            product.Id,
            DateTime.UtcNow,
            string.Empty,
            product.Name.Value,
            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity,
            product.CategoryId,
            product.StockQuantity > 0));

        return product;
    }

    public void Update(ProductName name, string description, Money price)
    {
        Name = name;
        Description = description ?? string.Empty;
        Price = price;
        ModifiedAt = DateTime.UtcNow;

        _domainEvents.Add(new ProductUpdatedEvent(
            Id,
            DateTime.UtcNow,
            string.Empty,
            Name.Value,
            Price.Amount,
            Price.Currency,
            StockQuantity,
            CategoryId,
            StockQuantity > 0));
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException("Stock quantity cannot be negative");
        }

        StockQuantity = quantity;
        ModifiedAt = DateTime.UtcNow;

        _domainEvents.Add(new ProductUpdatedEvent(
            Id,
            DateTime.UtcNow,
            string.Empty,
            Name.Value,
            Price.Amount,
            Price.Currency,
            StockQuantity,
            CategoryId,
            StockQuantity > 0));
    }

    public bool ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            return false;
        }

        if (StockQuantity < quantity)
        {
            return false;
        }

        StockQuantity -= quantity;
        ModifiedAt = DateTime.UtcNow;

        // Note: StockReservedEvent is published by the consumer after all items are reserved
        return true;
    }

    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero");
        }

        StockQuantity += quantity;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Release quantity must be positive");
        }

        StockQuantity += quantity;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

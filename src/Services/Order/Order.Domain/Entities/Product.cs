using BuildingBlocks.Common.Domain;

namespace Order.Domain.Entities;

/// <summary>
/// Read model for Product data synchronized from Catalog service via events.
/// </summary>
public sealed class Product : BaseEntity
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTime LastSyncedAt { get; private set; }

    private Product()
    {
        Name = string.Empty;
        Currency = "USD";
    }

    public static Product Create(
        Guid productId,
        string name,
        decimal price,
        string currency,
        bool isAvailable)
    {
        return new Product
        {
            Id = productId,
            Name = name,
            Price = price,
            Currency = currency,
            IsAvailable = isAvailable,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, decimal price, string currency, bool isAvailable)
    {
        Name = name;
        Price = price;
        Currency = currency;
        IsAvailable = isAvailable;
        LastSyncedAt = DateTime.UtcNow;
    }

    public void MarkAsUnavailable()
    {
        IsAvailable = false;
        LastSyncedAt = DateTime.UtcNow;
    }
}

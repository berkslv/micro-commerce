using BuildingBlocks.Common.Domain;
using BuildingBlocks.Common.Exceptions;

namespace Catalog.Domain.ValueObjects;

/// <summary>
/// Value object representing a product name.
/// </summary>
public sealed class ProductName : ValueObject
{
    public string Value { get; private set; } = string.Empty;

    private ProductName()
    {
    }

    public static ProductName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Product name is required");
        }

        if (value.Length > 200)
        {
            throw new DomainException("Product name cannot exceed 200 characters");
        }

        return new ProductName { Value = value };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(ProductName productName)
    {
        return productName.Value;
    }
}

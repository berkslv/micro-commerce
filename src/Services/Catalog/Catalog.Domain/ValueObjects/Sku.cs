using BuildingBlocks.Common.Domain;
using BuildingBlocks.Common.Exceptions;

namespace Catalog.Domain.ValueObjects;

/// <summary>
/// Value object representing a Stock Keeping Unit.
/// </summary>
public sealed class Sku : ValueObject
{
    public string Value { get; private set; } = string.Empty;

    private Sku()
    {
    }

    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("SKU is required");
        }

        if (value.Length > 50)
        {
            throw new DomainException("SKU cannot exceed 50 characters");
        }

        return new Sku { Value = value.ToUpperInvariant() };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(Sku sku)
    {
        return sku.Value;
    }
}

using BuildingBlocks.Common.Domain;
using BuildingBlocks.Common.Exceptions;

namespace Catalog.Domain.ValueObjects;

/// <summary>
/// Value object representing money with amount and currency.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "USD";

    private Money()
    {
    }

    public static Money Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
        {
            throw new DomainException("Price cannot be negative");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required");
        }

        return new Money { Amount = amount, Currency = currency.ToUpperInvariant() };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString()
    {
        return $"{Amount:F2} {Currency}";
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new DomainException("Cannot add money with different currencies");
        }

        return Create(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator *(Money money, int multiplier)
    {
        return Create(money.Amount * multiplier, money.Currency);
    }
}

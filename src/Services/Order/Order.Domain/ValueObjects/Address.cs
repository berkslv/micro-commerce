using BuildingBlocks.Common.Domain;
using BuildingBlocks.Common.Exceptions;

namespace Order.Domain.ValueObjects;

/// <summary>
/// Represents a shipping address value object.
/// </summary>
public sealed class Address : ValueObject
{
    public string Street { get; }

    public string City { get; }

    public string State { get; }

    public string Country { get; }

    public string ZipCode { get; }

    private Address(string street, string city, string state, string country, string zipCode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
    }

    public static Address Create(string street, string city, string state, string country, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new DomainException("Street is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("City is required.");
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new DomainException("State is required.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new DomainException("Country is required.");
        }

        if (string.IsNullOrWhiteSpace(zipCode))
        {
            throw new DomainException("Zip code is required.");
        }

        return new Address(street.Trim(), city.Trim(), state.Trim(), country.Trim(), zipCode.Trim());
    }

    public string FullAddress
    {
        get
        {
            return $"{Street}, {City}, {State} {ZipCode}, {Country}";
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }

    /// <summary>
    /// EF Core parameterless constructor
    /// </summary>
    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        State = string.Empty;
        Country = string.Empty;
        ZipCode = string.Empty;
    }
}

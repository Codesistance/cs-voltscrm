namespace VoltsCRM.Domain.Common;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string Region { get; }
    public string Country { get; }

    public Address(string street, string city, string region, string country)
    {
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(country)) throw new ArgumentException("Country is required.", nameof(country));

        Street = street?.Trim() ?? string.Empty;
        City = city.Trim();
        Region = region?.Trim() ?? string.Empty;
        Country = country.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Region;
        yield return Country;
    }
}

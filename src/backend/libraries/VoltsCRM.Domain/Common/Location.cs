namespace VoltsCRM.Domain.Common;

/// <summary>
/// A physical location: a postal <see cref="Address"/> plus optional GPS
/// <see cref="GpsCoordinates"/> (set manually or resolved from the address via geocoding).
/// Owned per record by entities such as <c>Agent</c> and <c>Customer</c>.
/// </summary>
public sealed class Location : ValueObject
{
    public Address Address { get; private set; }
    public GpsCoordinates? Coordinates { get; private set; }

    // EF Core: materializes nested owned types (Address/Coordinates) via this ctor, then the navigations.
    private Location() => Address = null!;

    public Location(Address address, GpsCoordinates? coordinates = null)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Coordinates = coordinates;
    }

    public Location WithCoordinates(GpsCoordinates? coordinates) => new(Address, coordinates);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Address;
        yield return Coordinates;
    }
}

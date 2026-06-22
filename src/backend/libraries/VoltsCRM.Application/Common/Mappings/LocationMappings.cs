using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;

namespace VoltsCRM.Application.Common.Mappings;

/// <summary>
/// Conversions between the shared Location transfer/input shapes and the domain value objects.
/// Shared by any feature that owns a <see cref="Location"/> (Customer, Agent, ServiceLocation).
/// </summary>
public static class LocationMappings
{
    public static AddressDto ToDto(this Address a) => new(a.Street, a.City, a.Region, a.Country);

    public static GpsCoordinatesDto? ToDto(this GpsCoordinates? c) =>
        c is null ? null : new(c.Latitude, c.Longitude);

    public static LocationDto ToDto(this Location l) => new(l.Address.ToDto(), l.Coordinates.ToDto());

    public static Address ToAddress(AddressInput i) =>
        new(i.Street ?? string.Empty, i.City, i.Region ?? string.Empty, i.Country);

    public static GpsCoordinates? ToCoordinates(GpsCoordinatesInput? i) =>
        i is null ? null : new(i.Latitude, i.Longitude);

    public static Location ToLocation(LocationInput i) =>
        new(ToAddress(i.Address), ToCoordinates(i.Coordinates));
}

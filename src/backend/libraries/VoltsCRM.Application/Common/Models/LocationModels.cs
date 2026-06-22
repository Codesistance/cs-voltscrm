namespace VoltsCRM.Application.Common.Models;

// Shared address/location transfer + input shapes, used by any feature that owns a Location
// (Customer, Agent, ServiceLocation). Kept here so the API and frontend use one consistent shape.

public sealed record AddressDto(string Street, string City, string Region, string Country);

public sealed record GpsCoordinatesDto(double Latitude, double Longitude);

public sealed record LocationDto(AddressDto Address, GpsCoordinatesDto? Coordinates);

// --- request inputs (nested value objects) ---
public sealed record AddressInput(string? Street, string City, string? Region, string Country);

public sealed record GpsCoordinatesInput(double Latitude, double Longitude);

public sealed record LocationInput(AddressInput Address, GpsCoordinatesInput? Coordinates);

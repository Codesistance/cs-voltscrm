using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Application.Features.Customers;

/// <summary>DTO for customer geo data on the map.</summary>
public sealed record CustomerGeoDto(
    Guid Id,
    string FullName,
    string AccountNumber,
    double? Latitude,
    double? Longitude,
    string City);

/// <summary>Result containing geo items within bounds.</summary>
public sealed record CustomerGeoResult(IReadOnlyList<CustomerGeoDto> Items);

/// <summary>Query for customers within geographic bounds.</summary>
public sealed record GetCustomersGeoQuery(
    double? MinLng,
    double? MinLat,
    double? MaxLng,
    double? MaxLat) : IRequest<CustomerGeoResult>;

public sealed class GetCustomersGeoHandler(IAppDbContext db) : IRequestHandler<GetCustomersGeoQuery, CustomerGeoResult>
{
    public async Task<CustomerGeoResult> Handle(GetCustomersGeoQuery query, CancellationToken ct)
    {
        var q = db.Customers.AsNoTracking()
            .Where(c => c.Location.Coordinates != null);

        // Apply bounds filter if all bounds are provided
        if (query.MinLng.HasValue && query.MinLat.HasValue &&
            query.MaxLng.HasValue && query.MaxLat.HasValue)
        {
            q = q.Where(c =>
                c.Location.Coordinates!.Longitude >= query.MinLng.Value &&
                c.Location.Coordinates!.Longitude <= query.MaxLng.Value &&
                c.Location.Coordinates!.Latitude >= query.MinLat.Value &&
                c.Location.Coordinates!.Latitude <= query.MaxLat.Value);
        }

        var customers = await q
            .Select(c => new CustomerGeoDto(
                c.Id,
                c.PersonalInfo.FirstName + " " + c.PersonalInfo.LastName,
                c.AccountNumber,
                c.Location.Coordinates != null ? c.Location.Coordinates.Latitude : null,
                c.Location.Coordinates != null ? c.Location.Coordinates.Longitude : null,
                c.Location.Address.City))
            .Take(500) // Limit results for performance
            .ToListAsync(ct);

        return new CustomerGeoResult(customers);
    }
}

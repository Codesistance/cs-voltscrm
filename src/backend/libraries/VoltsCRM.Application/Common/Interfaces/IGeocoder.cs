namespace VoltsCRM.Application.Common.Interfaces;

/// <summary>
/// Resolves a free-text address into candidate geographic coordinates.
/// Implemented in Infrastructure (proxied so rate-limiting and provider policy stay server-side).
/// </summary>
public interface IGeocoder
{
    Task<IReadOnlyList<GeocodeResult>> SearchAsync(string query, CancellationToken ct);
}

public sealed record GeocodeResult(string DisplayName, double Latitude, double Longitude);

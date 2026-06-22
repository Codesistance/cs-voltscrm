namespace VoltsCRM.Application.Common.Options;

/// <summary>
/// Geocoding configuration. Bind from the "Geocoding" section. Defaults target the public
/// OpenStreetMap Nominatim service. Per the OSM usage policy a descriptive <see cref="UserAgent"/>
/// (including a contact address) is REQUIRED, and callers must stay under ~1 request/second —
/// hence <see cref="MinSecondsBetweenCalls"/> and server-side caching.
/// </summary>
public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>Sent as the HTTP User-Agent. MUST identify the app and a contact email per OSM policy.</summary>
    public string UserAgent { get; set; } = "VoltsCRM/1.0 (admin@voltscrm.local)";

    /// <summary>Maximum candidate results returned per query.</summary>
    public int MaxResults { get; set; } = 5;

    /// <summary>Minimum spacing between upstream calls, to respect the provider rate limit.</summary>
    public double MinSecondsBetweenCalls { get; set; } = 1.0;

    /// <summary>How long to cache a query's results.</summary>
    public int CacheMinutes { get; set; } = 60;
}

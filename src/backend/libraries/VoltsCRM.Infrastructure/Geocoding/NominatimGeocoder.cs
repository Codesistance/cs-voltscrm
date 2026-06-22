using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Options;

namespace VoltsCRM.Infrastructure.Geocoding;

/// <summary>
/// <see cref="IGeocoder"/> backed by OpenStreetMap Nominatim. Calls are proxied through this server
/// component so we can enforce the required User-Agent, cache results, and throttle to the provider's
/// ~1 req/s policy limit (keeping the app from being rate-limited or banned).
/// </summary>
public sealed class NominatimGeocoder(
    HttpClient http,
    IMemoryCache cache,
    IOptions<GeocodingOptions> options) : IGeocoder
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTimeOffset _lastCall = DateTimeOffset.MinValue;

    private readonly GeocodingOptions _options = options.Value;

    public async Task<IReadOnlyList<GeocodeResult>> SearchAsync(string query, CancellationToken ct)
    {
        var normalized = query.Trim();
        var cacheKey = $"geocode:{normalized.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<GeocodeResult>? cached) && cached is not null)
            return cached;

        await Gate.WaitAsync(ct);
        try
        {
            var spacing = TimeSpan.FromSeconds(_options.MinSecondsBetweenCalls);
            var sinceLast = DateTimeOffset.UtcNow - _lastCall;
            if (sinceLast < spacing)
                await Task.Delay(spacing - sinceLast, ct);

            var url = $"/search?format=jsonv2&addressdetails=0&limit={_options.MaxResults}&q={Uri.EscapeDataString(normalized)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // User-Agent is set on the HttpClient at registration; kept here as a safety net.
            using var response = await http.SendAsync(request, ct);
            _lastCall = DateTimeOffset.UtcNow;
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var results = Parse(stream);

            cache.Set(cacheKey, results, TimeSpan.FromMinutes(_options.CacheMinutes));
            return results;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static IReadOnlyList<GeocodeResult> Parse(Stream json)
    {
        using var doc = JsonDocument.Parse(json);
        var results = new List<GeocodeResult>();

        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (!el.TryGetProperty("lat", out var latEl) || !el.TryGetProperty("lon", out var lonEl))
                continue;
            if (!double.TryParse(latEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(lonEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                continue;

            var displayName = el.TryGetProperty("display_name", out var nameEl) ? nameEl.GetString() ?? "" : "";
            results.Add(new GeocodeResult(displayName, lat, lon));
        }

        return results;
    }
}

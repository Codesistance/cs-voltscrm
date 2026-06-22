using MediatR;
using VoltsCRM.Application.Features.Geocoding;

namespace VoltsCRM.API.Endpoints;

/// <summary>
/// Server-side proxy for address → coordinates lookups, backing the optional "locate on map"
/// control on the Agent and Customer forms. Proxied (rather than called from the browser) so the
/// provider's User-Agent/rate-limit policy stays under our control. Requires authentication so the
/// endpoint can't be abused as an open geocoding relay.
/// </summary>
public static class GeocodingEndpoints
{
    public static IEndpointRouteBuilder MapGeocodingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/geocoding").WithTags("Geocoding");

        group.MapGet("/search", SearchAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> SearchAsync(ISender sender, string q, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GeocodeAddressQuery(q), ct));
}

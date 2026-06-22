using FluentValidation;
using MediatR;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Application.Features.Geocoding;

/// <summary>
/// Looks up candidate coordinates for a free-text address. Backs the optional "locate on map"
/// feature on the Agent and Customer forms.
/// </summary>
public sealed record GeocodeAddressQuery(string Q) : IRequest<IReadOnlyList<GeocodeResult>>;

public sealed class GeocodeAddressValidator : AbstractValidator<GeocodeAddressQuery>
{
    public GeocodeAddressValidator()
    {
        RuleFor(x => x.Q).NotEmpty().MinimumLength(3).MaximumLength(200);
    }
}

public sealed class GeocodeAddressHandler(IGeocoder geocoder)
    : IRequestHandler<GeocodeAddressQuery, IReadOnlyList<GeocodeResult>>
{
    public Task<IReadOnlyList<GeocodeResult>> Handle(GeocodeAddressQuery query, CancellationToken ct)
        => geocoder.SearchAsync(query.Q, ct);
}

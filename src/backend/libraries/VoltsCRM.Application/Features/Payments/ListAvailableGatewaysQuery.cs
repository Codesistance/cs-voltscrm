using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Application.Features.Payments;

/// <summary>A gateway offered to customers — no secrets. Returned only when visible AND implemented.</summary>
public sealed record AvailableGatewayDto(string KeyName, string DisplayName);

/// <summary>Lists gateways a customer may pay with: config rows with Visibility=true whose key also has
/// an implemented adapter (the "implemented ∩ visible" rule).</summary>
public sealed record ListAvailableGatewaysQuery : IRequest<IReadOnlyList<AvailableGatewayDto>>;

public sealed class ListAvailableGatewaysHandler(IAppDbContext db, IPaymentGatewayCatalog catalog)
    : IRequestHandler<ListAvailableGatewaysQuery, IReadOnlyList<AvailableGatewayDto>>
{
    public async Task<IReadOnlyList<AvailableGatewayDto>> Handle(ListAvailableGatewaysQuery query, CancellationToken ct)
    {
        var visible = await db.PaymentGatewayConfigs.AsNoTracking()
            .Where(c => c.Visibility)
            .Select(c => new { c.KeyName, c.DisplayName })
            .ToListAsync(ct);

        return visible
            .Where(c => catalog.ImplementedKeys.Contains(c.KeyName))
            .Select(c => new AvailableGatewayDto(c.KeyName, c.DisplayName))
            .ToList();
    }
}

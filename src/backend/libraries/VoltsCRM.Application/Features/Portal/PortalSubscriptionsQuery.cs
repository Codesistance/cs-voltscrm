using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;

namespace VoltsCRM.Application.Features.Portal;

public sealed record PortalSubscriptionsQuery(Guid CustomerId, int Page, int PageSize)
    : IRequest<PagedResult<PortalSubscriptionDto>>;

public sealed class PortalSubscriptionsQueryHandler(IAppDbContext db)
    : IRequestHandler<PortalSubscriptionsQuery, PagedResult<PortalSubscriptionDto>>
{
    public async Task<PagedResult<PortalSubscriptionDto>> Handle(PortalSubscriptionsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var baseQuery = db.CustomerSubscriptions.AsNoTracking()
            .Where(s => s.CustomerId == query.CustomerId);

        var total = await baseQuery.CountAsync(ct);
        var subscriptions = await baseQuery
            .OrderByDescending(s => s.StartDate)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var planIds = subscriptions.Select(s => s.ServicePlanId).Distinct().ToList();
        var plans = await db.ServicePlans.AsNoTracking()
            .Where(p => planIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, Amount = p.BasePrice.Amount, Currency = p.BasePrice.Currency })
            .ToListAsync(ct);
        var planLookup = plans.ToDictionary(p => p.Id);

        var items = subscriptions.Select(s =>
            {
                planLookup.TryGetValue(s.ServicePlanId, out var plan);
                var price = s.NegotiatedPrice ?? new Money(plan?.Amount ?? 0m, plan?.Currency ?? Money.DefaultCurrency);
                return new PortalSubscriptionDto(
                    s.Id,
                    s.ServicePlanId,
                    plan?.Name ?? "—",
                    s.Status.ToString(),
                    s.StartDate,
                    s.EndDate,
                    new MoneyDto(price.Amount, price.Currency));
            })
            .ToList();

        return new PagedResult<PortalSubscriptionDto>(items, page, size, total);
    }
}

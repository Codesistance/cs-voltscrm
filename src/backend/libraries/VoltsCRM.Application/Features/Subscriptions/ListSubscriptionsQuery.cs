using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Subscriptions;

public sealed record ListSubscriptionsQuery(
    int Page, int PageSize, string? Status, Guid? CustomerId, Guid? ServicePlanId)
    : IRequest<PagedResult<SubscriptionListItemDto>>;

public sealed class ListSubscriptionsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListSubscriptionsQuery, PagedResult<SubscriptionListItemDto>>
{
    public async Task<PagedResult<SubscriptionListItemDto>> Handle(ListSubscriptionsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<CustomerSubscription> q = db.CustomerSubscriptions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<SubscriptionStatus>(query.Status, true, out var status))
            q = q.Where(s => s.Status == status);
        if (query.CustomerId is { } customerId)
            q = q.Where(s => s.CustomerId == customerId);
        if (query.ServicePlanId is { } planId)
            q = q.Where(s => s.ServicePlanId == planId);

        var total = await q.CountAsync(ct);
        var subs = await q.OrderByDescending(s => s.StartDate)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        var customerIds = subs.Select(s => s.CustomerId).Distinct().ToList();
        var planIds = subs.Select(s => s.ServicePlanId).Distinct().ToList();

        var customerMap = (await db.Customers.AsNoTracking()
                .Where(c => customerIds.Contains(c.Id))
                .Select(c => new { c.Id, c.AccountNumber, c.PersonalInfo.FirstName, c.PersonalInfo.LastName })
                .ToListAsync(ct))
            .ToDictionary(c => c.Id, c => (Name: $"{c.FirstName} {c.LastName}", c.AccountNumber));

        var planMap = await db.ServicePlans.AsNoTracking()
            .Where(p => planIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var items = subs.Select(s =>
        {
            customerMap.TryGetValue(s.CustomerId, out var cust);
            return s.ToListItem(cust.Name ?? "—", cust.AccountNumber ?? "—",
                planMap.GetValueOrDefault(s.ServicePlanId, "—"));
        }).ToList();

        return new PagedResult<SubscriptionListItemDto>(items, page, size, total);
    }
}

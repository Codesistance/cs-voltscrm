using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Subscriptions;

public static class SubscriptionMapping
{
    public static MoneyDto ToMoneyDto(this Money money) => new(money.Amount, money.Currency);

    public static SubscriptionListItemDto ToListItem(
        this CustomerSubscription s, string customerName, string accountNumber, string planName) =>
        new(s.Id, s.CustomerId, customerName, accountNumber, s.ServicePlanId, planName,
            s.BillingType.ToString(), s.Status.ToString(), s.StartDate, s.EndDate);

    /// <summary>
    /// Builds the full subscription detail, resolving customer + plan names and deployed-item
    /// inventory code/name (ignoring soft-delete so dispatched-then-deactivated SKUs still render).
    /// </summary>
    public static async Task<SubscriptionDto> ToDetailDtoAsync(
        this CustomerSubscription s, IAppDbContext db, string defaultCurrency, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == s.CustomerId)
            .Select(c => new { c.AccountNumber, c.PersonalInfo.FirstName, c.PersonalInfo.LastName })
            .FirstOrDefaultAsync(ct);

        var plan = await db.ServicePlans.AsNoTracking()
            .Where(p => p.Id == s.ServicePlanId)
            .Select(p => new { p.PlanCode, p.Name, Amount = p.BasePrice.Amount, Currency = p.BasePrice.Currency })
            .FirstOrDefaultAsync(ct);

        var basePrice = plan is null ? new MoneyDto(0m, defaultCurrency) : new MoneyDto(plan.Amount, plan.Currency);
        var negotiated = s.NegotiatedPrice?.ToMoneyDto();

        var itemIds = s.DeployedItems.Select(d => d.InventoryItemId).Distinct().ToList();
        var lookup = itemIds.Count == 0
            ? new Dictionary<Guid, (string Code, string Name)>()
            : (await db.InventoryItems.AsNoTracking().IgnoreQueryFilters()
                    .Where(i => itemIds.Contains(i.Id))
                    .Select(i => new { i.Id, i.ItemCode, i.Name })
                    .ToListAsync(ct))
                .ToDictionary(i => i.Id, i => (Code: i.ItemCode, Name: i.Name));

        var deployed = s.DeployedItems
            .Select(d =>
            {
                lookup.TryGetValue(d.InventoryItemId, out var item);
                return new DeployedItemDto(d.Id, d.InventoryItemId,
                    item.Code ?? "—", item.Name ?? "(deleted item)",
                    d.SerialNumber, d.StockMovementId, d.DispatchedDate);
            })
            .ToList();

        return new SubscriptionDto(
            s.Id, s.CustomerId,
            customer is null ? "—" : $"{customer.FirstName} {customer.LastName}",
            customer?.AccountNumber ?? "—",
            s.ServicePlanId, plan?.PlanCode ?? "—", plan?.Name ?? "—",
            s.BillingType.ToString(), s.Status.ToString(),
            negotiated, negotiated ?? basePrice,
            s.StartDate, s.EndDate, deployed, s.CreatedAt, s.UpdatedAt);
    }
}

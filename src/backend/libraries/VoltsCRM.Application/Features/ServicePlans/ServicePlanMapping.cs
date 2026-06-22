using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.ServicePlans;

public static class ServicePlanMapping
{
    public static MoneyDto ToMoneyDto(this Money money) => new(money.Amount, money.Currency);

    public static ServicePlanListItemDto ToListItem(this ServicePlan p) => new(
        p.Id, p.PlanCode, p.Name, p.BillingType.ToString(), p.BillingCycle.ToString(),
        p.BasePrice.ToMoneyDto(), p.Status.ToString(), p.LineItems.Count);

    /// <summary>
    /// Builds the full plan detail, resolving each line item's inventory code/name (soft-deleted
    /// inventory items are still resolved so archived SKUs in a plan still render).
    /// </summary>
    public static async Task<ServicePlanDto> ToDetailDtoAsync(
        this ServicePlan p, IAppDbContext db, CancellationToken ct)
    {
        var lookup = await InventoryLookup.LoadAsync(db, p.LineItems.Select(l => l.InventoryItemId), ct);

        var lineItems = p.LineItems
            .Select(l =>
            {
                lookup.TryGetValue(l.InventoryItemId, out var item);
                return new ServicePlanLineItemDto(
                    l.Id, l.InventoryItemId,
                    item.Code ?? "—",
                    item.Name ?? "(deleted item)",
                    l.Quantity, l.Role.ToString());
            })
            .ToList();

        return new ServicePlanDto(
            p.Id, p.PlanCode, p.Name, p.Description,
            p.BillingType.ToString(), p.BillingCycle.ToString(),
            p.BasePrice.ToMoneyDto(), p.Status.ToString(),
            lineItems, p.CreatedAt, p.UpdatedAt);
    }
}

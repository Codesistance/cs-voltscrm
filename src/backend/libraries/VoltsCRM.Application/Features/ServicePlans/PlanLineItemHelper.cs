using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Application.Features.ServicePlans;

internal static class PlanLineItemHelper
{
    /// <summary>Throws a validation error if any supplied line item references a missing inventory item.</summary>
    public static async Task EnsureItemsExistAsync(
        IAppDbContext db, IReadOnlyList<ServicePlanLineItemInput> lineItems, CancellationToken ct)
    {
        var ids = lineItems.Select(l => l.InventoryItemId).Distinct().ToList();
        if (ids.Count == 0) return;

        var existing = await db.InventoryItems
            .Where(i => ids.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync(ct);

        var missing = ids.Except(existing).ToList();
        if (missing.Count != 0)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(ServicePlanLineItemInput.InventoryItemId),
                    "One or more selected inventory items do not exist."),
            });
    }
}

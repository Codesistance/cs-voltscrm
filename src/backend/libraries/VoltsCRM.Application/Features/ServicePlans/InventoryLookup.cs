using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Application.Features.ServicePlans;

/// <summary>Loads code/name for a set of inventory items (ignoring soft-delete so removed items still display).</summary>
internal static class InventoryLookup
{
    public static async Task<IReadOnlyDictionary<Guid, (string Code, string Name)>> LoadAsync(
        IAppDbContext db, IEnumerable<Guid> inventoryItemIds, CancellationToken ct)
    {
        var ids = inventoryItemIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, (string, string)>();

        var items = await db.InventoryItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => ids.Contains(i.Id))
            .Select(i => new { i.Id, i.ItemCode, i.Name })
            .ToListAsync(ct);

        return items.ToDictionary(i => i.Id, i => (i.ItemCode, i.Name));
    }
}

using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory;

public static class InventoryMapping
{
    public static MoneyDto ToDto(this Money money) => new(money.Amount, money.Currency);

    public static InventoryItemDto ToDto(this InventoryItem i) => new(
        i.Id, i.ItemCode, i.Name, i.Description, i.CategoryId, i.Category?.Name ?? string.Empty,
        i.Category?.TracksStock ?? true, i.UnitOfMeasure, i.QuantityOnHand, i.ReorderLevel,
        i.UnitCost.ToDto(), i.IsActive, IsLowStock(i), i.CreatedAt, i.UpdatedAt);

    public static InventoryItemListItemDto ToListItem(this InventoryItem i) => new(
        i.Id, i.ItemCode, i.Name, i.CategoryId, i.Category?.Name ?? string.Empty, i.UnitOfMeasure,
        i.QuantityOnHand, i.ReorderLevel, i.UnitCost.ToDto(), IsLowStock(i));

    public static StockMovementDto ToDto(this StockMovement s) => new(
        s.Id, s.InventoryItemId, s.MovementType.ToString(), s.Quantity,
        s.Reference, s.Notes, s.MovedByUserId, s.CreatedAt);

    private static bool IsLowStock(InventoryItem i) =>
        i is { QuantityOnHand: { } qty, ReorderLevel: { } reorder } && qty <= reorder;
}

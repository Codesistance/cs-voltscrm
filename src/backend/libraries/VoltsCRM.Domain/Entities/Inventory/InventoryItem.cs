using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Inventory;

public class InventoryItem : Entity
{
    public string ItemCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public InventoryCategory? Category { get; private set; }
    public string UnitOfMeasure { get; private set; } = "unit";
    public int? QuantityOnHand { get; private set; }
    public int? ReorderLevel { get; private set; }
    public Money UnitCost { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? DeletedAt { get; private set; }

    private InventoryItem() { }

    public InventoryItem(string itemCode, string name, InventoryCategory category, Money unitCost,
        string unitOfMeasure = "unit", string? description = null, int? quantityOnHand = null, int? reorderLevel = null)
    {
        ItemCode = itemCode;
        Name = name;
        CategoryId = category.Id;
        Category = category;
        UnitCost = unitCost;
        UnitOfMeasure = unitOfMeasure;
        Description = description;

        if (category.TracksStock)
        {
            QuantityOnHand = quantityOnHand ?? 0;
            ReorderLevel = reorderLevel;
        }
    }

    /// <summary>Adjusts on-hand stock. No-op for items whose category does not track stock.</summary>
    public void AdjustStock(int delta, bool tracksStock)
    {
        if (!tracksStock) return;
        QuantityOnHand = (QuantityOnHand ?? 0) + delta;
    }

    public void Update(string name, string? description, Money unitCost, int? reorderLevel, bool tracksStock)
    {
        Name = name;
        Description = description;
        UnitCost = unitCost;
        if (tracksStock) ReorderLevel = reorderLevel;
    }

    public void Deactivate() { IsActive = false; DeletedAt = DateTimeOffset.UtcNow; }
}

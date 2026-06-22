using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Inventory;

/// <summary>
/// Admin-managed classification for inventory items. <see cref="TracksStock"/> controls whether items
/// in this category carry quantity/reorder levels (false = a service-style, non-stock category).
/// </summary>
public class InventoryCategory : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public bool TracksStock { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? DeletedAt { get; private set; }

    private InventoryCategory() { }

    public InventoryCategory(string name, bool tracksStock = true, string? code = null)
    {
        Name = name;
        TracksStock = tracksStock;
        Code = code;
    }

    public void Update(string name, string? code, bool tracksStock)
    {
        Name = name;
        Code = code;
        TracksStock = tracksStock;
    }

    public void Deactivate()
    {
        IsActive = false;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}

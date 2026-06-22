using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Inventory;

public class StockMovement : Entity
{
    public Guid InventoryItemId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public int Quantity { get; private set; }
    public string? Reference { get; private set; }
    public Guid? RelatedSubscriptionId { get; private set; }
    public string? MovedByUserId { get; private set; }
    public string? Notes { get; private set; }

    private StockMovement() { }

    public StockMovement(Guid inventoryItemId, StockMovementType movementType, int quantity,
        string? reference = null, Guid? relatedSubscriptionId = null, string? movedByUserId = null, string? notes = null)
    {
        InventoryItemId = inventoryItemId;
        MovementType = movementType;
        Quantity = quantity;
        Reference = reference;
        RelatedSubscriptionId = relatedSubscriptionId;
        MovedByUserId = movedByUserId;
        Notes = notes;
    }
}

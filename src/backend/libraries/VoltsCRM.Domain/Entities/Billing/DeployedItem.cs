using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Billing;

public class DeployedItem : Entity
{
    public Guid SubscriptionId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateTimeOffset DispatchedDate { get; private set; }
    public Guid StockMovementId { get; private set; }

    private DeployedItem() { }

    public DeployedItem(Guid subscriptionId, Guid inventoryItemId, Guid stockMovementId, string? serialNumber = null)
    {
        SubscriptionId = subscriptionId;
        InventoryItemId = inventoryItemId;
        StockMovementId = stockMovementId;
        SerialNumber = serialNumber;
        DispatchedDate = DateTimeOffset.UtcNow;
    }
}

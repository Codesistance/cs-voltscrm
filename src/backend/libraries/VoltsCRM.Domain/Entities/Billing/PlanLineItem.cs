using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Billing;

public class PlanLineItem : Entity
{
    public Guid ServicePlanId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public int Quantity { get; private set; }
    public PlanLineItemRole Role { get; private set; }

    private PlanLineItem() { }

    public PlanLineItem(Guid servicePlanId, Guid inventoryItemId, int quantity, PlanLineItemRole role)
    {
        ServicePlanId = servicePlanId;
        InventoryItemId = inventoryItemId;
        Quantity = quantity;
        Role = role;
    }
}

using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Billing;

public class ServicePlan : Entity
{
    public string PlanCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public BillingType BillingType { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public Money BasePrice { get; private set; } = null!;
    public PlanStatus Status { get; private set; } = PlanStatus.Active;

    private readonly List<PlanLineItem> _lineItems = [];
    public IReadOnlyList<PlanLineItem> LineItems => _lineItems.AsReadOnly();

    private ServicePlan() { }

    public ServicePlan(string planCode, string name, BillingType billingType, BillingCycle billingCycle, Money basePrice, string? description = null)
    {
        PlanCode = planCode;
        Name = name;
        BillingType = billingType;
        BillingCycle = billingCycle;
        BasePrice = basePrice;
        Description = description;
    }

    public void AddLineItem(Guid inventoryItemId, int quantity, PlanLineItemRole role)
    {
        _lineItems.Add(new PlanLineItem(Id, inventoryItemId, quantity, role));
    }

    public void RemoveLineItem(Guid lineItemId)
    {
        var item = _lineItems.FirstOrDefault(l => l.Id == lineItemId);
        if (item is not null) _lineItems.Remove(item);
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void Archive() => Status = PlanStatus.Archived;
    public void Restore() => Status = PlanStatus.Active;
    public void UpdatePrice(Money newPrice) => BasePrice = newPrice;
}

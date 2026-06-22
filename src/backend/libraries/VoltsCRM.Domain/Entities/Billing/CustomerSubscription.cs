using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Billing;

public class CustomerSubscription : Entity
{
    public Guid CustomerId { get; private set; }
    public Guid ServicePlanId { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public BillingType BillingType { get; private set; }
    public Money? NegotiatedPrice { get; private set; }
    public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.Pending;

    private readonly List<DeployedItem> _deployedItems = [];
    public IReadOnlyList<DeployedItem> DeployedItems => _deployedItems.AsReadOnly();

    private CustomerSubscription() { }

    public CustomerSubscription(Guid customerId, Guid servicePlanId, BillingType billingType,
        DateTimeOffset startDate, Money? negotiatedPrice = null)
    {
        CustomerId = customerId;
        ServicePlanId = servicePlanId;
        BillingType = billingType;
        StartDate = startDate;
        NegotiatedPrice = negotiatedPrice;
    }

    public void Activate()
    {
        if (Status != SubscriptionStatus.Pending)
            throw new InvalidOperationException("Only pending subscriptions can be activated.");
        Status = SubscriptionStatus.Active;
    }

    public void UpdatePendingDetails(DateTimeOffset startDate, Money? negotiatedPrice)
    {
        if (Status != SubscriptionStatus.Pending)
            throw new InvalidOperationException("Only pending subscriptions can be edited.");
        StartDate = startDate;
        NegotiatedPrice = negotiatedPrice;
    }

    public DeployedItem RecordDispatchedItem(Guid inventoryItemId, Guid stockMovementId, string? serialNumber = null)
    {
        var deployed = new DeployedItem(Id, inventoryItemId, stockMovementId, serialNumber);
        _deployedItems.Add(deployed);
        return deployed;
    }

    public void Suspend()
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Only active subscriptions can be suspended.");
        Status = SubscriptionStatus.Suspended;
    }

    public void Resume()
    {
        if (Status != SubscriptionStatus.Suspended)
            throw new InvalidOperationException("Only suspended subscriptions can be resumed.");
        Status = SubscriptionStatus.Active;
    }

    public void Terminate(DateTimeOffset terminatedAt)
    {
        Status = SubscriptionStatus.Terminated;
        EndDate = terminatedAt;
    }
}

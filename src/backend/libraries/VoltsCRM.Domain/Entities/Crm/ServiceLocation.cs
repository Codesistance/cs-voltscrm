using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Crm;

public class ServiceLocation : Entity
{
    public Guid CustomerId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public Location Location { get; private set; } = null!;
    public Guid? ActiveSubscriptionId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ServiceLocation() { }

    public ServiceLocation(Guid customerId, string label, Location location)
    {
        CustomerId = customerId;
        Label = label;
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }

    public void LinkSubscription(Guid subscriptionId) => ActiveSubscriptionId = subscriptionId;
    public void Deactivate() => IsActive = false;
}

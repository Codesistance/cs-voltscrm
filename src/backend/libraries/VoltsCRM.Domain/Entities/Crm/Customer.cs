using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Crm;

public class Customer : Entity
{
    public string AccountNumber { get; private set; } = string.Empty;
    public PersonalInfo PersonalInfo { get; private set; } = null!;
    public Location Location { get; private set; } = null!;
    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;

    private readonly List<ServiceLocation> _serviceLocations = [];
    public IReadOnlyList<ServiceLocation> ServiceLocations => _serviceLocations.AsReadOnly();

    private Customer() { }

    public Customer(string accountNumber, PersonalInfo personalInfo, Location location)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) throw new ArgumentException("Account number is required.", nameof(accountNumber));

        AccountNumber = accountNumber.Trim();
        PersonalInfo = personalInfo ?? throw new ArgumentNullException(nameof(personalInfo));
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }

    public ServiceLocation AddServiceLocation(string label, Location location)
    {
        var serviceLocation = new ServiceLocation(Id, label, location);
        _serviceLocations.Add(serviceLocation);
        return serviceLocation;
    }

    public void UpdatePersonalInfo(PersonalInfo info) => PersonalInfo = info;
    public void UpdateLocation(Location location) => Location = location ?? throw new ArgumentNullException(nameof(location));
    public void Suspend() => Status = CustomerStatus.Suspended;
    public void Reactivate() => Status = CustomerStatus.Active;
    public void Disconnect() => Status = CustomerStatus.Disconnected;
}

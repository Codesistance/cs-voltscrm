using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Organisation;

/// <summary>
/// Field-servicing staff profile. 1:1 with an <c>AspNetUsers</c> login (UserType = Agent).
/// </summary>
public class Agent : Entity
{
    public string UserId { get; private set; } = string.Empty;
    public Location Location { get; private set; } = null!;
    public string? Territory { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Agent() { }

    public Agent(string userId, Location location, string? territory = null)
    {
        UserId = userId;
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Territory = territory;
    }

    public void Update(Location location, string? territory)
    {
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Territory = territory;
    }

    public void UpdateTerritory(string territory) => Territory = territory;
    public void Deactivate() => IsActive = false;
}

using Microsoft.AspNetCore.Identity;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>The kind of user this login is — drives UI area routing and which profile table applies.</summary>
    public UserType UserType { get; set; } = UserType.Customer;

    /// <summary>Set when <see cref="UserType"/> is <see cref="UserType.Customer"/>; links to the CRM customer profile.</summary>
    public Guid? CustomerId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>When true, the user must set a new password before they can use the app (e.g. after an invite or admin reset).</summary>
    public bool MustChangePassword { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

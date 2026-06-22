namespace VoltsCRM.Infrastructure.Identity;

/// <summary>
/// Profile row for an <see cref="AppUser"/> whose <c>UserType</c> is <c>Administration</c>.
/// Holds the admin-specific data and the link to the dynamic RBAC roles. Lives in the
/// <c>identity</c> schema alongside the ASP.NET Identity tables. Customers and agents
/// have their own profile tables (<c>Customer</c>, <c>Agent</c>) instead.
/// </summary>
public class AdministrationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <see cref="AppUser"/>.Id (1:1).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Super admins implicitly hold every permission and can manage access control.</summary>
    public bool IsSuperAdmin { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<AdminUserRole> Roles { get; set; } = [];
}

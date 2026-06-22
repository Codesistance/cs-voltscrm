namespace VoltsCRM.Infrastructure.Identity;

/// <summary>
/// A dynamic, runtime-managed role for Administration users. Each role bundles a set of
/// <see cref="Permission"/>s and can be assigned to many admins. Created and edited via the
/// access-management UI; <see cref="IsSystem"/> roles (e.g. "Super Administrator") are protected
/// from deletion.
/// </summary>
public class AdminRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Built-in roles that must not be deleted (guards against lockout).</summary>
    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<AdminRolePermission> Permissions { get; set; } = [];
    public ICollection<AdminUserRole> Users { get; set; } = [];
}

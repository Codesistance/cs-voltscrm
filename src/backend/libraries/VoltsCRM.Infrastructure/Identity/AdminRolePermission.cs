namespace VoltsCRM.Infrastructure.Identity;

/// <summary>Join row granting one <see cref="Permission"/> to one <see cref="AdminRole"/>.</summary>
public class AdminRolePermission
{
    public Guid AdminRoleId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
}

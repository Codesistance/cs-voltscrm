namespace VoltsCRM.Infrastructure.Identity;

/// <summary>Join row assigning one <see cref="AdminRole"/> to one <see cref="AdministrationUser"/>.</summary>
public class AdminUserRole
{
    public Guid AdministrationUserId { get; set; }
    public Guid AdminRoleId { get; set; }
}

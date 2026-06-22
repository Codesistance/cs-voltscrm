using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.Integration.Tests.Helpers;

/// <summary>Arrange-phase helpers that create real users + profile/RBAC rows in the test database.</summary>
public static class TestUsers
{
    public const string DefaultPassword = "Test123!pass";

    public sealed record SeededAdmin(string UserId, Guid AdminProfileId, Guid RoleId);

    /// <summary>
    /// Creates a (non-super) Administration user holding a fresh role that grants the given permissions,
    /// and returns the identifiers needed to mint a matching token / call access-management endpoints.
    /// </summary>
    public static async Task<SeededAdmin> CreateAdminWithRoleAsync(
        IServiceProvider sp, IEnumerable<string> permissions, string? email = null)
    {
        email ??= $"admin_{Guid.NewGuid():N}@voltscrm.local";
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var db = sp.GetRequiredService<AppDbContext>();

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "Admin",
            UserType = UserType.Administration,
            IsActive = true,
        };
        var result = await userManager.CreateAsync(user, DefaultPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create test admin: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));

        var role = new AdminRole { Name = $"Role_{Guid.NewGuid():N}", Description = "Test role" };
        foreach (var key in permissions.Distinct())
            role.Permissions.Add(new AdminRolePermission { AdminRoleId = role.Id, PermissionKey = key });
        db.AdminRoles.Add(role);

        var profile = new AdministrationUser { UserId = user.Id, IsSuperAdmin = false };
        profile.Roles.Add(new AdminUserRole { AdministrationUserId = profile.Id, AdminRoleId = role.Id });
        db.AdministrationUsers.Add(profile);

        await db.SaveChangesAsync();
        return new SeededAdmin(user.Id, profile.Id, role.Id);
    }

    /// <summary>
    /// Creates a super-admin user holding a fresh role that grants the given permissions.
    /// Super admins can grant the IsSuperAdmin flag to newly created admins.
    /// </summary>
    public static async Task<SeededAdmin> CreateSuperAdminWithRoleAsync(
        IServiceProvider sp, IEnumerable<string> permissions, string? email = null)
    {
        email ??= $"superadmin_{Guid.NewGuid():N}@voltscrm.local";
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var db = sp.GetRequiredService<AppDbContext>();

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Super",
            LastName = "Admin",
            UserType = UserType.Administration,
            IsActive = true,
        };
        var result = await userManager.CreateAsync(user, DefaultPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create test super admin: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));

        var role = new AdminRole { Name = $"Role_{Guid.NewGuid():N}", Description = "Test role" };
        foreach (var key in permissions.Distinct())
            role.Permissions.Add(new AdminRolePermission { AdminRoleId = role.Id, PermissionKey = key });
        db.AdminRoles.Add(role);

        var profile = new AdministrationUser { UserId = user.Id, IsSuperAdmin = true };
        profile.Roles.Add(new AdminUserRole { AdministrationUserId = profile.Id, AdminRoleId = role.Id });
        db.AdministrationUsers.Add(profile);

        await db.SaveChangesAsync();
        return new SeededAdmin(user.Id, profile.Id, role.Id);
    }
}

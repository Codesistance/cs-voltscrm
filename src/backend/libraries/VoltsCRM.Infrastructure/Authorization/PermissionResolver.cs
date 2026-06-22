using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.Infrastructure.Authorization;

/// <inheritdoc />
public sealed class PermissionResolver(AppDbContext db) : IPermissionResolver
{
    public async Task<IReadOnlySet<string>> GetPermissionsAsync(string userId, CancellationToken ct = default)
    {
        var profile = await db.AdministrationUsers
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.Id, a.IsSuperAdmin })
            .FirstOrDefaultAsync(ct);

        if (profile is null)
            return new HashSet<string>();

        // Super admins implicitly hold every code-defined permission.
        if (profile.IsSuperAdmin)
            return Permissions.Keys.ToHashSet();

        var keys = await db.AdminUserRoles
            .AsNoTracking()
            .Where(ur => ur.AdministrationUserId == profile.Id)
            .Join(db.AdminRolePermissions,
                ur => ur.AdminRoleId,
                rp => rp.AdminRoleId,
                (ur, rp) => rp.PermissionKey)
            .Distinct()
            .ToListAsync(ct);

        // Defensive: only surface keys that still exist in the code-defined catalogue.
        return keys.Where(Permissions.Keys.Contains).ToHashSet();
    }
}

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.API.Auth;

/// <summary>Shared authorization checks for admin endpoints that go beyond the permission policies.</summary>
public static class AdminAuthorization
{
    /// <summary>True when the calling principal maps to an active super-admin administration profile.</summary>
    public static async Task<bool> IsSuperAdminAsync(AppDbContext db, ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is not null
            && await db.AdministrationUsers.AnyAsync(a => a.UserId == userId && a.IsSuperAdmin, ct);
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.API.Endpoints;

/// <summary>
/// Runtime management of the dynamic admin RBAC: the permission catalogue, admin roles, and which
/// roles each Administration user holds. Every endpoint requires the <c>access.manage</c> permission.
/// </summary>
public static class AccessEndpoints
{
    public static IEndpointRouteBuilder MapAccessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/access")
            .WithTags("Access")
            .RequirePermission(Permissions.AccessManage);

        group.MapGet("/permissions", GetPermissions);
        group.MapGet("/roles", GetRolesAsync);
        group.MapPost("/roles", CreateRoleAsync);
        group.MapPut("/roles/{id:guid}", UpdateRoleAsync);
        group.MapDelete("/roles/{id:guid}", DeleteRoleAsync);
        group.MapGet("/admins", GetAdminsAsync);
        group.MapPost("/admins", CreateAdminAsync);
        group.MapPut("/admins/{id:guid}/roles", AssignRolesAsync);
        group.MapPost("/admins/{id:guid}/reset-password", ResetAdminPasswordAsync);

        return app;
    }

    private static IResult GetPermissions()
        => Results.Ok(Permissions.All.Select(p => new PermissionDto(p.Key, p.Group, p.Description)));

    private static async Task<IResult> GetRolesAsync(AppDbContext db, CancellationToken ct)
    {
        var roles = await db.AdminRoles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new AdminRoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystem,
                r.Permissions.Select(p => p.PermissionKey).ToList(),
                r.Users.Count))
            .ToListAsync(ct);

        return Results.Ok(roles);
    }

    private static async Task<IResult> CreateRoleAsync(SaveAdminRoleRequest request, AppDbContext db, CancellationToken ct)
    {
        if (Invalid(request, out var problem)) return problem;

        if (await db.AdminRoles.AnyAsync(r => r.Name == request.Name, ct))
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: $"A role named '{request.Name}' already exists.");

        var role = new AdminRole { Name = request.Name.Trim(), Description = request.Description };
        foreach (var key in request.Permissions.Distinct())
            role.Permissions.Add(new AdminRolePermission { AdminRoleId = role.Id, PermissionKey = key });

        db.AdminRoles.Add(role);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/admin/access/roles/{role.Id}",
            new AdminRoleDto(role.Id, role.Name, role.Description, role.IsSystem,
                role.Permissions.Select(p => p.PermissionKey).ToList(), 0));
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid id, SaveAdminRoleRequest request, AppDbContext db, CancellationToken ct)
    {
        if (Invalid(request, out var problem)) return problem;

        var role = await db.AdminRoles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null) return Results.NotFound();
        if (role.IsSystem)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "System roles cannot be modified.");

        if (await db.AdminRoles.AnyAsync(r => r.Name == request.Name && r.Id != id, ct))
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: $"A role named '{request.Name}' already exists.");

        role.Name = request.Name.Trim();
        role.Description = request.Description;

        var desired = request.Permissions.Distinct().ToHashSet();
        foreach (var stale in role.Permissions.Where(p => !desired.Contains(p.PermissionKey)).ToList())
            role.Permissions.Remove(stale);
        var held = role.Permissions.Select(p => p.PermissionKey).ToHashSet();
        foreach (var key in desired.Where(k => !held.Contains(k)))
            role.Permissions.Add(new AdminRolePermission { AdminRoleId = role.Id, PermissionKey = key });

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteRoleAsync(Guid id, AppDbContext db, CancellationToken ct)
    {
        var role = await db.AdminRoles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null) return Results.NotFound();
        if (role.IsSystem)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "System roles cannot be deleted.");

        db.AdminRoles.Remove(role); // cascades to role-permission and user-role joins
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAdminsAsync(AppDbContext db, CancellationToken ct)
    {
        var admins = await db.AdministrationUsers
            .AsNoTracking()
            .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new { a, u })
            .OrderBy(x => x.u.Email)
            .Select(x => new AdminUserDto(
                x.a.Id,
                x.a.UserId,
                x.u.Email ?? string.Empty,
                x.u.FirstName + " " + x.u.LastName,
                x.a.IsSuperAdmin,
                x.a.Roles.Select(r => r.AdminRoleId).ToList()))
            .ToListAsync(ct);

        return Results.Ok(admins);
    }

    private static async Task<IResult> CreateAdminAsync(
        CreateAdminRequest request,
        UserManager<AppUser> userManager,
        AppDbContext db,
        IEmailSender email,
        IOptions<EmailOptions> emailOptions,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Email is required.");

        // Granting super-admin requires the caller to be a super admin.
        if (request.IsSuperAdmin)
        {
            var callerUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerIsSuper = callerUserId is not null
                && await db.AdministrationUsers.AnyAsync(a => a.UserId == callerUserId && a.IsSuperAdmin, ct);
            if (!callerIsSuper)
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Only a super admin can grant super-admin status.");
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        if (roleIds.Count > 0)
        {
            var existing = await db.AdminRoles.CountAsync(r => roleIds.Contains(r.Id), ct);
            if (existing != roleIds.Count)
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "One or more roles do not exist.");
        }

        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: $"A user with email '{request.Email}' already exists.");

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserType = UserType.Administration,
            IsActive = true,
            MustChangePassword = true,
        };

        var result = await userManager.CreateAsync(user, PasswordGenerator.GenerateTemporary());
        if (!result.Succeeded)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, "Administrator");

        var profile = new AdministrationUser { UserId = user.Id, IsSuperAdmin = request.IsSuperAdmin };
        foreach (var roleId in roleIds)
            profile.Roles.Add(new AdminUserRole { AdministrationUserId = profile.Id, AdminRoleId = roleId });
        db.AdministrationUsers.Add(profile);
        await db.SaveChangesAsync(ct);

        await AccountInviteService.SendSetPasswordInviteAsync(userManager, email, emailOptions.Value, user);

        var dto = new AdminUserDto(profile.Id, user.Id, user.Email!, user.FullName, profile.IsSuperAdmin,
            roleIds);
        return Results.Created($"/api/admin/access/admins/{profile.Id}", dto);
    }

    private static async Task<IResult> ResetAdminPasswordAsync(
        Guid id, ResetPasswordRequest request, UserManager<AppUser> userManager, AppDbContext db, CancellationToken ct)
    {
        var admin = await db.AdministrationUsers.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (admin is null) return Results.NotFound();

        var user = await userManager.FindByIdAsync(admin.UserId);
        if (user is null) return Results.NotFound();

        var (succeeded, generated, errors) = await AccountInviteService.ResetPasswordAsync(userManager, user, request.NewPassword);
        if (!succeeded)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: string.Join("; ", errors.Select(e => e.Description)));

        return Results.Ok(new ResetPasswordResult(generated));
    }

    private static async Task<IResult> AssignRolesAsync(
        Guid id, AssignRolesRequest request, AppDbContext db, ClaimsPrincipal principal, CancellationToken ct)
    {
        var admin = await db.AdministrationUsers.Include(a => a.Roles).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (admin is null) return Results.NotFound();

        var roleIds = request.RoleIds.Distinct().ToList();
        var existingCount = await db.AdminRoles.CountAsync(r => roleIds.Contains(r.Id), ct);
        if (existingCount != roleIds.Count)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "One or more roles do not exist.");

        // Self-lockout guard: a non-super admin must not strip their own access-management permission.
        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (admin.UserId == currentUserId && !admin.IsSuperAdmin)
        {
            var willRetainAccessManage = await db.AdminRolePermissions
                .AnyAsync(rp => roleIds.Contains(rp.AdminRoleId) && rp.PermissionKey == Permissions.AccessManage, ct);
            if (!willRetainAccessManage)
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                    title: "You cannot remove your own access-management permission.");
        }

        admin.Roles.Clear();
        foreach (var roleId in roleIds)
            admin.Roles.Add(new AdminUserRole { AdministrationUserId = admin.Id, AdminRoleId = roleId });

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static bool Invalid(SaveAdminRoleRequest request, out IResult problem)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            problem = Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Role name is required.");
            return true;
        }

        var unknown = request.Permissions.Where(p => !Permissions.Keys.Contains(p)).ToList();
        if (unknown.Count > 0)
        {
            problem = Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: $"Unknown permission(s): {string.Join(", ", unknown)}.");
            return true;
        }

        problem = Results.Empty;
        return false;
    }
}

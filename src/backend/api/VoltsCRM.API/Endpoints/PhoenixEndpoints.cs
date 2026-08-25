using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.API.Endpoints;

/// <summary>
/// Phoenix — a super-admin break-glass account-recovery endpoint. Given a user's email it
/// <b>resets</b> that account to a freshly generated temporary password (returned once so it can be
/// handed over out-of-band), forces a change at next login, and re-activates the account if it was
/// disabled. It never reveals an existing password — passwords are stored only as salted hashes, so
/// there is nothing to reveal, and issuing a fresh credential is the only safe recovery primitive.
/// <para>
/// The whole module is gated by <see cref="PhoenixOptions.Enabled"/> (the <c>enable_phoenix</c>
/// tfvar). When the flag is off, <see cref="MapPhoenixEndpoints"/> maps nothing and every Phoenix
/// path 404s — the route does not exist. When on, the endpoint still requires an authenticated
/// super admin, so the flag widens nothing on its own.
/// </para>
/// </summary>
public static class PhoenixEndpoints
{
    public static IEndpointRouteBuilder MapPhoenixEndpoints(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<IOptions<PhoenixOptions>>().Value;
        if (!options.Enabled)
            return app; // Flag off → nothing mapped → the path does not exist (404).

        var group = app.MapGroup("/api/admin/phoenix")
            .WithTags("Phoenix")
            .RequireRateLimiting("auth")
            .RequirePermission(Permissions.AccessManage);

        group.MapPost("/reset", ResetByEmailAsync);

        return app;
    }

    private static async Task<IResult> ResetByEmailAsync(
        PhoenixResetRequest request,
        UserManager<AppUser> userManager,
        AppDbContext db,
        ClaimsPrincipal principal,
        IAuditLogger audit,
        CancellationToken ct)
    {
        // Super-admin only. access.manage (required by the group) is necessary but not sufficient:
        // recovery of any account is a super-admin action, mirroring the admin disable/enable guard.
        if (!await AdminAuthorization.IsSuperAdminAsync(db, principal, ct))
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                title: "Only a super admin can use account recovery.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Email is required.");

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return Results.NotFound();

        // Reset to a generated temporary password (forces a change at next login). Passing null makes
        // AccountInviteService generate the value and return it so we can surface it once.
        var (succeeded, generated, errors) = await AccountInviteService.ResetPasswordAsync(userManager, user, null);
        if (!succeeded)
        {
            await audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.PhoenixRecover,
                Outcome = AuditOutcomes.Failure,
                TargetType = "user",
                TargetId = user.Id,
                TargetLabel = user.Email,
                Details = JsonSerializer.Serialize(new { errors = errors.Select(e => e.Description) }),
            }, ct);
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: string.Join("; ", errors.Select(e => e.Description)));
        }

        // Recovery also re-activates a disabled account so the user can sign back in.
        var reactivated = false;
        if (!user.IsActive)
        {
            user.IsActive = true;
            await userManager.UpdateAsync(user);
            reactivated = true;
        }

        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.PhoenixRecover,
            Outcome = AuditOutcomes.Success,
            TargetType = "user",
            TargetId = user.Id,
            TargetLabel = user.Email,
            Details = JsonSerializer.Serialize(new { reactivated }),
        }, ct);

        return Results.Ok(new PhoenixResetResult(user.Email!, generated!, reactivated));
    }
}

/// <summary>Phoenix recovery request: the email of the account to recover.</summary>
public sealed record PhoenixResetRequest(string Email);

/// <summary>
/// Phoenix recovery result: the freshly generated temporary password (shown once) and whether a
/// disabled account was re-activated as part of recovery.
/// </summary>
public sealed record PhoenixResetResult(string Email, string TemporaryPassword, bool Reactivated);

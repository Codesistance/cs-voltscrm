using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.API.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshCookie = "refreshToken";
    private const string CookiePath = "/api/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth");

        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", Me).RequireAuthorization();
        group.MapPost("/set-password", SetPasswordAsync).AllowAnonymous();
        group.MapPost("/change-password", ChangePasswordAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        IPermissionResolver permissionResolver,
        AppDbContext db,
        HttpContext http,
        IOptions<AuthOptions> authOptions,
        CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionResolver.GetPermissionsAsync(user.Id, ct);
        var isSuperAdmin = await db.AdministrationUsers.AnyAsync(a => a.UserId == user.Id && a.IsSuperAdmin, ct);
        var access = tokenService.CreateAccessToken(user, roles, permissions);
        var refresh = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.Hash,
            ExpiresAt = refresh.ExpiresAt,
        });
        await db.SaveChangesAsync(ct);

        var inBody = authOptions.Value.RefreshTokenInBody;
        if (!inBody)
            SetRefreshCookie(http, refresh.Value, refresh.ExpiresAt);

        return Results.Ok(new LoginResponse(
            access.Value,
            access.ExpiresInSeconds,
            new AuthUserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                user.UserType.ToString(),
                roles.ToList(),
                permissions.ToList(),
                user.MustChangePassword,
                isSuperAdmin),
            inBody ? refresh.Value : null));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        IPermissionResolver permissionResolver,
        AppDbContext db,
        HttpContext http,
        IOptions<AuthOptions> authOptions,
        CancellationToken ct)
    {
        var inBody = authOptions.Value.RefreshTokenInBody;
        var raw = inBody ? request.RefreshToken : http.Request.Cookies[RefreshCookie];
        if (string.IsNullOrEmpty(raw))
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Missing refresh token.");

        var now = DateTimeOffset.UtcNow;
        var hash = tokenService.Hash(raw);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null || !stored.IsActive(now))
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token.");

        var user = await userManager.FindByIdAsync(stored.UserId);
        if (user is null || !user.IsActive)
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token.");

        // Rotate: revoke the current token and issue a fresh one.
        var next = tokenService.CreateRefreshToken();
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = next.Hash;
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = next.Hash, ExpiresAt = next.ExpiresAt });
        await db.SaveChangesAsync(ct);

        // Re-resolve permissions so admin role changes take effect on the next refresh (≤ access-token lifetime).
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionResolver.GetPermissionsAsync(user.Id, ct);
        var access = tokenService.CreateAccessToken(user, roles, permissions);
        if (!inBody)
            SetRefreshCookie(http, next.Value, next.ExpiresAt);

        return Results.Ok(new RefreshResponse(access.Value, access.ExpiresInSeconds, inBody ? next.Value : null));
    }

    private static async Task<IResult> LogoutAsync(
        RefreshRequest? request,
        ITokenService tokenService,
        AppDbContext db,
        HttpContext http,
        IOptions<AuthOptions> authOptions,
        CancellationToken ct)
    {
        var inBody = authOptions.Value.RefreshTokenInBody;
        var raw = inBody ? request?.RefreshToken : http.Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(raw))
        {
            var hash = tokenService.Hash(raw);
            var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (stored is not null && stored.RevokedAt is null)
            {
                stored.RevokedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        if (!inBody)
            DeleteRefreshCookie(http);
        return Results.NoContent();
    }

    private static async Task<IResult> Me(ClaimsPrincipal principal, UserManager<AppUser> userManager, AppDbContext db)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null)
            return Results.Unauthorized();

        var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = principal.FindFirstValue("name") ?? string.Empty;
        var userType = principal.FindFirstValue(AppClaims.UserType) ?? string.Empty;
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = principal.FindAll(AppClaims.Permission).Select(c => c.Value).ToList();

        // MustChangePassword / IsSuperAdmin aren't claims — read them so a bootstrap (silent refresh) still surfaces them.
        var user = await userManager.FindByIdAsync(id);
        var mustChange = user?.MustChangePassword ?? false;
        var isSuperAdmin = await db.AdministrationUsers.AnyAsync(a => a.UserId == id && a.IsSuperAdmin);

        return Results.Ok(new AuthUserDto(id, email, name, userType, roles, permissions, mustChange, isSuperAdmin));
    }

    private static async Task<IResult> SetPasswordAsync(
        SetPasswordRequest request, UserManager<AppUser> userManager)
    {
        // Generic failure message — don't reveal whether the email exists.
        const string genericError = "That password reset link is invalid or has expired.";

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: genericError);

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: result.Errors.Any(e => e.Code.Contains("Password"))
                    ? string.Join("; ", result.Errors.Select(e => e.Description))
                    : genericError);

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request, ClaimsPrincipal principal, UserManager<AppUser> userManager)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null) return Results.Unauthorized();

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.Unauthorized();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: string.Join("; ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        return Results.NoContent();
    }

    // Local dev runs over HTTPS (Vite https proxy → https API), so the cookie is always Secure.
    private static void SetRefreshCookie(HttpContext http, string value, DateTimeOffset expires)
        => http.Response.Cookies.Append(RefreshCookie, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = expires,
            IsEssential = true,
        });

    private static void DeleteRefreshCookie(HttpContext http)
        => http.Response.Cookies.Delete(RefreshCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
        });
}

using Microsoft.AspNetCore.Identity;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.API.Auth;

/// <summary>
/// Sends a "set your password" invite: generates an Identity reset token, builds the SPA link, and
/// emails it. Shared by agent and administrator provisioning. The recipient sets their password via the
/// public <c>/api/auth/set-password</c> endpoint, which clears <c>MustChangePassword</c>.
/// </summary>
public static class AccountInviteService
{
    public static async Task SendSetPasswordInviteAsync(
        UserManager<AppUser> userManager, IEmailSender email, EmailOptions options, AppUser user)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = $"{options.AppBaseUrl.TrimEnd('/')}/set-password" +
            $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

        var body = $"""
            <p>Hi {user.FirstName},</p>
            <p>An account has been created for you on VoltsCRM. Set your password to get started:</p>
            <p><a href="{link}">Set your password</a></p>
            <p>If the link doesn't work, copy and paste this URL into your browser:</p>
            <p>{link}</p>
            """;

        await email.SendAsync(user.Email!, "Set up your VoltsCRM account", body);
    }

    /// <summary>
    /// Resets a user's password directly — no email round trip — to either an admin-supplied value
    /// or a freshly generated one, and forces a change at next login. Use when email delivery isn't
    /// wired up (or as a same-page alternative to the invite email even when it is).
    /// </summary>
    /// <returns>
    /// The generated password when <paramref name="newPassword"/> was null/empty (so the caller can
    /// show it once), or null when the caller supplied their own value (nothing new to disclose).
    /// </returns>
    public static async Task<(bool Succeeded, string? GeneratedPassword, IEnumerable<IdentityError> Errors)> ResetPasswordAsync(
        UserManager<AppUser> userManager, AppUser user, string? newPassword)
    {
        var generated = string.IsNullOrWhiteSpace(newPassword);
        var password = generated ? PasswordGenerator.GenerateTemporary() : newPassword!;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
            return (false, null, result.Errors);

        user.MustChangePassword = true;
        await userManager.UpdateAsync(user);
        return (true, generated ? password : null, []);
    }
}

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
}

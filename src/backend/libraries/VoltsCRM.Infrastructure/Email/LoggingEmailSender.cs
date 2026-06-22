using Microsoft.Extensions.Logging;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Infrastructure.Email;

/// <summary>
/// Dev/fallback email sender. Logs the message (including any links) instead of sending it, so local
/// development and environments without SES configured still exercise the full flow.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("[Email:NOOP] To={To} Subject={Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}

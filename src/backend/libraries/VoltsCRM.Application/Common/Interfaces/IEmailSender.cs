namespace VoltsCRM.Application.Common.Interfaces;

/// <summary>Sends transactional emails (e.g. agent invites). Implemented in Infrastructure.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

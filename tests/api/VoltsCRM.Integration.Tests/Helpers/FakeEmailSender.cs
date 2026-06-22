using System.Collections.Concurrent;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Integration.Tests.Helpers;

/// <summary>
/// Test double that captures all sent emails for later assertion. Thread-safe for parallel test scenarios.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentBag<SentEmail> _sentEmails = [];

    public IReadOnlyList<SentEmail> SentEmails => _sentEmails.ToList();

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _sentEmails.Add(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }

    public void Clear() => _sentEmails.Clear();

    public sealed record SentEmail(string To, string Subject, string HtmlBody);
}

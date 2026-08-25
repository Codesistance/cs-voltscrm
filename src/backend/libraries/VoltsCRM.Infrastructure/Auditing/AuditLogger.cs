using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.Infrastructure.Auditing;

/// <summary>
/// Persists audit records to the append-only <c>audit_events</c> table and, at the same time, emits a
/// structured copy to the application log so it also lands in CloudWatch (a second, out-of-band trail
/// that survives even if the database is tampered with). Actor/IP/user-agent are read from the current
/// request unless the caller supplied them.
/// </summary>
public sealed class AuditLogger(
    AppDbContext db,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogger> logger) : IAuditLogger
{
    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext;
        var actorUserId = entry.ActorUserId ?? http?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorEmail = entry.ActorEmail ?? http?.User.FindFirstValue(ClaimTypes.Email);
        var ip = ClientIp(http);
        var userAgent = Truncate(http?.Request.Headers.UserAgent.ToString(), 512);

        var ev = AuditEvent.Create(
            timeProvider.GetUtcNow(), entry.Action, entry.Outcome,
            actorUserId, actorEmail,
            entry.TargetType, entry.TargetId, entry.TargetLabel,
            ip, userAgent, entry.Details);

        db.AuditEvents.Add(ev);
        await db.SaveChangesAsync(ct);

        // Second, out-of-band copy: stdout → CloudWatch Logs via the ECS awslogs driver.
        logger.LogInformation(
            "AUDIT {Action} {Outcome} actor={ActorEmail}({ActorUserId}) target={TargetType}:{TargetId}({TargetLabel}) ip={Ip} details={Details}",
            entry.Action, entry.Outcome, actorEmail, actorUserId,
            entry.TargetType, entry.TargetId, entry.TargetLabel, ip, entry.Details);
    }

    private static string? ClientIp(HttpContext? http)
    {
        if (http is null) return null;
        // Behind the ALB/CloudFront the socket peer is the proxy; the real client is the first
        // X-Forwarded-For hop. Fall back to the socket address for direct (local/dev) requests.
        var forwarded = http.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return Truncate(forwarded.Split(',')[0].Trim(), 45);
        return http.Connection.RemoteIpAddress?.ToString();
    }

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? null : (s.Length <= max ? s : s[..max]);
}

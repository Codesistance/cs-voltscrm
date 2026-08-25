namespace VoltsCRM.Infrastructure.Auditing;

/// <summary>
/// An append-only record of a security-sensitive action (who did what to whom, from where, when,
/// and whether it succeeded). Records are only ever inserted — the <c>AuditInterceptor</c> rejects
/// any attempt to modify or delete one, so the trail is tamper-resistant at the persistence layer.
/// Written exclusively through <c>IAuditLogger</c>; a second out-of-band copy is emitted to the
/// application log (→ CloudWatch) at the same time.
/// </summary>
public sealed class AuditEvent
{
    private AuditEvent() { }

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>When the action occurred (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Stable, dotted action name, e.g. <c>phoenix.recover</c> or <c>auth.login_failed</c>.</summary>
    public string Action { get; private set; } = default!;

    /// <summary><c>success</c> or <c>failure</c>.</summary>
    public string Outcome { get; private set; } = default!;

    public string? ActorUserId { get; private set; }
    public string? ActorEmail { get; private set; }

    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? TargetLabel { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    /// <summary>Optional JSON blob of extra structured context (stored as jsonb).</summary>
    public string? Details { get; private set; }

    public static AuditEvent Create(
        DateTimeOffset occurredAt, string action, string outcome,
        string? actorUserId, string? actorEmail,
        string? targetType, string? targetId, string? targetLabel,
        string? ipAddress, string? userAgent, string? details)
        => new()
        {
            OccurredAt = occurredAt,
            Action = action,
            Outcome = outcome,
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            TargetType = targetType,
            TargetId = targetId,
            TargetLabel = targetLabel,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
        };
}

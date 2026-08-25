namespace VoltsCRM.Application.Common.Interfaces;

/// <summary>Well-known audit action names. Dotted, lowercase, stable — safe to filter on.</summary>
public static class AuditActions
{
    public const string PhoenixRecover = "phoenix.recover";
    public const string AdminCreate = "admin.create";
    public const string AdminResetPassword = "admin.reset_password";
    public const string AdminDisable = "admin.disable";
    public const string AdminEnable = "admin.enable";
    public const string AdminAssignRoles = "admin.assign_roles";
    public const string RoleCreate = "role.create";
    public const string RoleUpdate = "role.update";
    public const string RoleDelete = "role.delete";
    public const string LoginSucceeded = "auth.login_succeeded";
    public const string LoginFailed = "auth.login_failed";
}

public static class AuditOutcomes
{
    public const string Success = "success";
    public const string Failure = "failure";
}

/// <summary>
/// A single audit record to append. Actor identity, client IP, user-agent and timestamp are filled
/// in by the logger from the current request when not supplied here (callers may override the actor
/// for pre-authentication events such as a failed login).
/// </summary>
public sealed record AuditEntry
{
    public required string Action { get; init; }
    public string Outcome { get; init; } = AuditOutcomes.Success;
    public string? ActorUserId { get; init; }
    public string? ActorEmail { get; init; }
    public string? TargetType { get; init; }
    public string? TargetId { get; init; }
    public string? TargetLabel { get; init; }

    /// <summary>Optional JSON string of extra context.</summary>
    public string? Details { get; init; }
}

/// <summary>Appends security-audit records. Implementations must be append-only.</summary>
public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
}

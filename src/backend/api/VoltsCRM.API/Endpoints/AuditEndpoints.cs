using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using VoltsCRM.API.Auth;
using VoltsCRM.Infrastructure.Auditing;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.API.Endpoints;

/// <summary>
/// Read-only access to the security audit trail. Super-admin only: the audit log records privileged
/// actions (including super admins' own), so it must not be readable by ordinary admins. There is no
/// write, update, or delete surface here — records are appended solely by <c>IAuditLogger</c>.
/// </summary>
public static class AuditEndpoints
{
    private const int MaxExportRows = 10_000;

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/audit")
            .WithTags("Audit")
            .RequireAuthorization();

        group.MapGet("", ListAsync);
        group.MapGet("/actions", ActionsAsync);
        group.MapGet("/export.csv", ExportCsvAsync);

        return app;
    }

    private static async Task<IResult> ListAsync(
        AppDbContext db, ClaimsPrincipal principal,
        string? action, string? actorEmail, string? targetId, string? outcome,
        DateTimeOffset? from, DateTimeOffset? to,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        if (!await AdminAuthorization.IsSuperAdminAsync(db, principal, ct))
            return Forbidden();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = Filter(db, action, actorEmail, targetId, outcome, from, to);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditEventDto(
                e.Id, e.OccurredAt, e.Action, e.Outcome,
                e.ActorUserId, e.ActorEmail,
                e.TargetType, e.TargetId, e.TargetLabel,
                e.IpAddress, e.UserAgent, e.Details))
            .ToListAsync(ct);

        return Results.Ok(new AuditPageDto(items, total, page, pageSize));
    }

    private static async Task<IResult> ActionsAsync(AppDbContext db, ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!await AdminAuthorization.IsSuperAdminAsync(db, principal, ct))
            return Forbidden();

        var actions = await db.AuditEvents
            .Select(e => e.Action).Distinct().OrderBy(a => a).ToListAsync(ct);
        return Results.Ok(actions);
    }

    private static async Task<IResult> ExportCsvAsync(
        AppDbContext db, ClaimsPrincipal principal,
        string? action, string? actorEmail, string? targetId, string? outcome,
        DateTimeOffset? from, DateTimeOffset? to,
        CancellationToken ct = default)
    {
        if (!await AdminAuthorization.IsSuperAdminAsync(db, principal, ct))
            return Forbidden();

        var rows = await Filter(db, action, actorEmail, targetId, outcome, from, to)
            .OrderByDescending(e => e.OccurredAt)
            .Take(MaxExportRows)
            .Select(e => new AuditEventDto(
                e.Id, e.OccurredAt, e.Action, e.Outcome,
                e.ActorUserId, e.ActorEmail,
                e.TargetType, e.TargetId, e.TargetLabel,
                e.IpAddress, e.UserAgent, e.Details))
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("OccurredAt,Action,Outcome,ActorEmail,ActorUserId,TargetType,TargetId,TargetLabel,IpAddress,UserAgent,Details");
        foreach (var r in rows)
        {
            sb.Append(Csv(r.OccurredAt.ToString("O"))).Append(',')
              .Append(Csv(r.Action)).Append(',')
              .Append(Csv(r.Outcome)).Append(',')
              .Append(Csv(r.ActorEmail)).Append(',')
              .Append(Csv(r.ActorUserId)).Append(',')
              .Append(Csv(r.TargetType)).Append(',')
              .Append(Csv(r.TargetId)).Append(',')
              .Append(Csv(r.TargetLabel)).Append(',')
              .Append(Csv(r.IpAddress)).Append(',')
              .Append(Csv(r.UserAgent)).Append(',')
              .Append(Csv(r.Details)).Append('\n');
        }

        return Results.File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "audit-log.csv");
    }

    private static IQueryable<AuditEvent> Filter(
        AppDbContext db, string? action, string? actorEmail, string? targetId, string? outcome,
        DateTimeOffset? from, DateTimeOffset? to)
    {
        var query = db.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(e => e.Action == action);
        if (!string.IsNullOrWhiteSpace(outcome))
            query = query.Where(e => e.Outcome == outcome);
        if (!string.IsNullOrWhiteSpace(actorEmail))
        {
            var needle = actorEmail.Trim().ToLower();
            query = query.Where(e => e.ActorEmail != null && e.ActorEmail.ToLower().Contains(needle));
        }
        if (!string.IsNullOrWhiteSpace(targetId))
            query = query.Where(e => e.TargetId == targetId);
        if (from is not null)
            query = query.Where(e => e.OccurredAt >= from);
        if (to is not null)
            query = query.Where(e => e.OccurredAt <= to);

        return query;
    }

    private static IResult Forbidden()
        => Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Only a super admin can view the audit log.");

    /// <summary>RFC 4180 CSV field: quote when needed and double any embedded quotes.</summary>
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var needsQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuote ? $"\"{escaped}\"" : escaped;
    }
}

public sealed record AuditEventDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string Action,
    string Outcome,
    string? ActorUserId,
    string? ActorEmail,
    string? TargetType,
    string? TargetId,
    string? TargetLabel,
    string? IpAddress,
    string? UserAgent,
    string? Details);

public sealed record AuditPageDto(IReadOnlyList<AuditEventDto> Items, int Total, int Page, int PageSize);

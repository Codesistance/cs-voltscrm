using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Auditing;
using VoltsCRM.Infrastructure.Persistence;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>Integration tests for the security audit trail: it records sensitive actions, is readable
/// only by super admins, and is append-only.</summary>
[Collection("SharedTestContainers")]
public class AuditTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(AuditTests);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<int> CountAuditAsync(string action, string? targetLabel = null)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var q = db.AuditEvents.AsNoTracking().Where(e => e.Action == action);
        if (targetLabel is not null) q = q.Where(e => e.TargetLabel == targetLabel);
        return await q.CountAsync(Ct);
    }

    [Fact]
    public async Task AdminCreate_WritesSuccessAuditEvent()
    {
        using var scope = Factory.CreateScopeForArrange();
        var superAdmin = await TestUsers.CreateSuperAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(superAdmin.UserId, UserType.Administration, [Permissions.AccessManage]);
        var newEmail = $"created_{Guid.NewGuid():N}@voltscrm.local";

        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = newEmail,
            firstName = "New",
            lastName = "Admin",
            roleIds = Array.Empty<Guid>(),
            isSuperAdmin = false,
        }, token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var verify = Factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = await db.AuditEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Action == AuditActions.AdminCreate && e.TargetLabel == newEmail, Ct);
        Assert.NotNull(ev);
        Assert.Equal(AuditOutcomes.Success, ev!.Outcome);
        Assert.False(string.IsNullOrEmpty(ev.ActorEmail));
        Assert.Equal("admin", ev.TargetType);
    }

    [Fact]
    public async Task LoginFailure_WritesFailureAuditEvent()
    {
        var email = $"ghost_{Guid.NewGuid():N}@voltscrm.local";
        var before = await CountAuditAsync(AuditActions.LoginFailed, email);

        var response = await PostAsync("/api/auth/login", new { email, password = "definitely-wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.Equal(before + 1, await CountAuditAsync(AuditActions.LoginFailed, email));
    }

    [Fact]
    public async Task AuditList_AsNonSuperAdmin_Returns403()
    {
        using var scope = Factory.CreateScopeForArrange();
        var admin = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        var response = await GetAsync("/api/admin/audit", token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuditList_AsSuperAdmin_ReturnsEvents()
    {
        using var scope = Factory.CreateScopeForArrange();
        var superAdmin = await TestUsers.CreateSuperAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(superAdmin.UserId, UserType.Administration, [Permissions.AccessManage]);

        // Generate at least one auditable event.
        await PostAsync("/api/auth/login", new { email = "nobody@voltscrm.local", password = "wrong" });

        var response = await GetAsync("/api/admin/audit?pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<AuditPageResponse>(Ct);
        Assert.NotNull(page);
        Assert.True(page!.Total >= 1);
        Assert.NotEmpty(page.Items);
    }

    [Fact]
    public async Task AuditEvents_AreAppendOnly()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ev = AuditEvent.Create(
            DateTimeOffset.UtcNow, "test.action", AuditOutcomes.Success,
            actorUserId: null, actorEmail: "t@voltscrm.local",
            targetType: "user", targetId: "1", targetLabel: "t@voltscrm.local",
            ipAddress: "127.0.0.1", userAgent: "test", details: null);
        db.AuditEvents.Add(ev);
        await db.SaveChangesAsync(Ct); // insert allowed

        // Modifying a persisted audit row must be rejected at the persistence layer.
        db.Entry(ev).State = EntityState.Modified;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));

        // Deleting one must be rejected too.
        db.Entry(ev).State = EntityState.Deleted;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
    }

    private sealed record AuditPageResponse(List<AuditItem> Items, int Total, int Page, int PageSize);
    private sealed record AuditItem(string Id, string Action, string Outcome, string? ActorEmail);
}

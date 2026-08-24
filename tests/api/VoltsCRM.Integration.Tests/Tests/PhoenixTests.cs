using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Integration tests for the Phoenix super-admin account-recovery endpoint
/// (POST /api/admin/phoenix/reset), with the feature flag ON.
/// </summary>
[Collection("SharedTestContainers")]
public class PhoenixTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(PhoenixTests);

    // Enable the Phoenix endpoints for this class (mirrors the enable_phoenix tfvar → Phoenix:Enabled).
    protected override IReadOnlyDictionary<string, string?>? ExtraConfig
        => new Dictionary<string, string?> { ["Phoenix:Enabled"] = "true" };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Creates a target account to be recovered, optionally disabled, and returns its email.</summary>
    private async Task<string> CreateTargetAsync(bool active)
    {
        var email = $"target_{Guid.NewGuid():N}@voltscrm.local";
        using var scope = Factory.CreateScopeForArrange();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Target",
            LastName = "User",
            UserType = UserType.Customer,
            IsActive = active,
        };
        var result = await userManager.CreateAsync(user, "OldPass123!");
        Assert.True(result.Succeeded);
        return email;
    }

    [Fact]
    public async Task Reset_AsSuperAdmin_IssuesTempPassword_ForcesChange_AndReactivates()
    {
        // Arrange: a super admin (with access.manage) and a disabled target account.
        using var scope = Factory.CreateScopeForArrange();
        var superAdmin = await TestUsers.CreateSuperAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(superAdmin.UserId, UserType.Administration, [Permissions.AccessManage]);
        var targetEmail = await CreateTargetAsync(active: false);

        // Act
        var response = await PostAsync("/api/admin/phoenix/reset", new { email = targetEmail }, token);

        // Assert: 200 with a fresh temporary password and reactivation flag.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await ReadAsync<PhoenixResetResultDtoT>(response);
        Assert.NotNull(dto);
        Assert.Equal(targetEmail, dto!.Email);
        Assert.False(string.IsNullOrWhiteSpace(dto.TemporaryPassword));
        Assert.True(dto.Reactivated);

        // The temporary password actually works, and the account is active + must-change.
        using var verify = Factory.Services.CreateScope();
        var userManager = verify.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Email == targetEmail, Ct);
        Assert.True(user.IsActive);
        Assert.True(user.MustChangePassword);
        var reloaded = await userManager.FindByEmailAsync(targetEmail);
        Assert.True(await userManager.CheckPasswordAsync(reloaded!, dto.TemporaryPassword));
    }

    [Fact]
    public async Task Reset_AsNonSuperAdmin_Returns403()
    {
        // Arrange: a non-super admin that still holds access.manage.
        using var scope = Factory.CreateScopeForArrange();
        var admin = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);
        var targetEmail = await CreateTargetAsync(active: true);

        // Act
        var response = await PostAsync("/api/admin/phoenix/reset", new { email = targetEmail }, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reset_UnknownEmail_Returns404()
    {
        using var scope = Factory.CreateScopeForArrange();
        var superAdmin = await TestUsers.CreateSuperAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(superAdmin.UserId, UserType.Administration, [Permissions.AccessManage]);

        var response = await PostAsync("/api/admin/phoenix/reset",
            new { email = $"nobody_{Guid.NewGuid():N}@voltscrm.local" }, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>
/// Verifies that with the Phoenix flag OFF (the default), the endpoint is not mapped at all —
/// the path does not exist.
/// </summary>
[Collection("SharedTestContainers")]
public class PhoenixDisabledTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(PhoenixDisabledTests);
    // No ExtraConfig → Phoenix:Enabled defaults to false.

    [Fact]
    public async Task Reset_WhenFlagDisabled_Returns404()
    {
        using var scope = Factory.CreateScopeForArrange();
        var superAdmin = await TestUsers.CreateSuperAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(superAdmin.UserId, UserType.Administration, [Permissions.AccessManage]);

        var response = await PostAsync("/api/admin/phoenix/reset", new { email = "anyone@voltscrm.local" }, token);

        // The route isn't registered, so it's a plain 404 (not a 401/403 from an existing endpoint).
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

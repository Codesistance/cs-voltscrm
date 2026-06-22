using System.Net;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>Covers the self-lockout Must-Fix: an admin must not strip their own access-management permission.</summary>
[Collection("SharedTestContainers")]
public class SelfLockoutTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(SelfLockoutTests);

    private async Task<TestUsers.SeededAdmin> SeedSelfManagingAdminAsync()
    {
        using var scope = Factory.CreateScopeForArrange();
        return await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
    }

    [Fact]
    public async Task RemovingOwnAccessManageRole_ReturnsBadRequest()
    {
        var admin = await SeedSelfManagingAdminAsync();
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        var response = await PutAsync(
            $"/api/admin/access/admins/{admin.AdminProfileId}/roles",
            new { roleIds = Array.Empty<Guid>() },
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task KeepingOwnAccessManageRole_Succeeds()
    {
        var admin = await SeedSelfManagingAdminAsync();
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        var response = await PutAsync(
            $"/api/admin/access/admins/{admin.AdminProfileId}/roles",
            new { roleIds = new[] { admin.RoleId } },
            token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}

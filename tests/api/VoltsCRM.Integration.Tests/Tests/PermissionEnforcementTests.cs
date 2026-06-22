using System.Net;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Proves server-side enforcement (the Must-Fix): the dynamic <c>perm:</c> policy provider denies/
/// allows purely on the signed token's claims, independent of any SPA guard.
/// </summary>
[Collection("SharedTestContainers")]
public class PermissionEnforcementTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(PermissionEnforcementTests);

    private static string Admin(params string[] perms) =>
        TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Administration, perms);

    [Fact]
    public async Task Inventory_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await GetAsync("/api/inventory");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inventory_CustomerToken_ReturnsForbidden()
    {
        var token = TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Customer);
        var response = await GetAsync("/api/inventory", token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Inventory_AdminWithoutViewPermission_ReturnsForbidden()
    {
        var response = await GetAsync("/api/inventory", Admin());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Inventory_AdminWithViewPermission_ReturnsOk()
    {
        var response = await GetAsync("/api/inventory", Admin(Permissions.InventoryView));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateInventory_AdminWithoutManagePermission_ReturnsForbidden()
    {
        // Authorization runs before model binding, so an empty body still yields 403 when the
        // permission is missing.
        var response = await PostAsync("/api/inventory", new { }, Admin(Permissions.InventoryView));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInventory_AdminWithManagePermission_PassesAuthorization()
    {
        // With the permission, authorization passes; an empty body then fails validation (not 403/401).
        var response = await PostAsync("/api/inventory", new { }, Admin(Permissions.InventoryManage));
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessManagement_NonAccessManageAdmin_ReturnsForbidden()
    {
        var response = await GetAsync("/api/admin/access/roles", Admin(Permissions.InventoryView));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

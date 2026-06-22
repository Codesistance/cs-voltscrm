using System.Net;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

[Collection("SharedTestContainers")]
public class AccessManagementTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(AccessManagementTests);

    private static string AccessManager() =>
        TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Administration, [Permissions.AccessManage]);

    [Fact]
    public async Task GetPermissions_ReturnsFullCatalogue()
    {
        var response = await GetAsync("/api/admin/access/permissions", AccessManager());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var perms = await ReadAsync<List<PermissionDtoT>>(response);
        Assert.NotNull(perms);
        Assert.Equal(Permissions.All.Count, perms!.Count);
    }

    [Fact]
    public async Task CreateRole_ThenAppearsInRolesList()
    {
        var token = AccessManager();
        var create = await PostAsync("/api/admin/access/roles", new
        {
            name = "Billing Clerk",
            description = "Handles invoices",
            permissions = new[] { Permissions.InvoicesView },
        }, token);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var roles = await ReadAsync<List<AdminRoleDtoT>>(await GetAsync("/api/admin/access/roles", token));
        Assert.NotNull(roles);
        var created = Assert.Single(roles!, r => r.Name == "Billing Clerk");
        Assert.Contains(Permissions.InvoicesView, created.Permissions);
    }

    [Fact]
    public async Task SystemRole_CannotBeUpdatedOrDeleted()
    {
        var token = AccessManager();
        var roles = await ReadAsync<List<AdminRoleDtoT>>(await GetAsync("/api/admin/access/roles", token));
        var systemRole = Assert.Single(roles!, r => r.IsSystem);

        var update = await PutAsync($"/api/admin/access/roles/{systemRole.Id}", new
        {
            name = "Renamed",
            description = "nope",
            permissions = Array.Empty<string>(),
        }, token);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);

        var delete = await DeleteAsync($"/api/admin/access/roles/{systemRole.Id}", token);
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }
}

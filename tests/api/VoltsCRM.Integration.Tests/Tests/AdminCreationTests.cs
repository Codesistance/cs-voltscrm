using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Persistence;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Integration tests for POST /api/admin/access/admins endpoint.
/// Tests admin creation, super-admin flag enforcement, validation, and email invite sending.
/// </summary>
[Collection("SharedTestContainers")]
public class AdminCreationTests : IAsyncLifetime
{
    private readonly SharedTestContainersFixture _fixture;
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private FakeEmailSender _fakeEmail = null!;

    public AdminCreationTests(SharedTestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _fakeEmail = new FakeEmailSender();
        _factory = new CustomWebApplicationFactory(
            _fixture.PostgresConnectionString,
            nameof(AdminCreationTests),
            _fakeEmail);
        await _factory.InitializeAsync();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> PostAsync(string url, object? body, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request, Ct);
    }

    private Task<HttpResponseMessage> GetAsync(string url, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request, Ct);
    }

    private static Task<T?> ReadAsync<T>(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<T>(Ct);

    [Fact]
    public async Task CreateAdmin_WithAccessManagePermission_Returns201AndCreatesRowsAndSendsInvite()
    {
        // Arrange: Create a non-super admin with access.manage
        using var scope = _factory.CreateScopeForArrange();
        var admin = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        var newEmail = $"newadmin_{Guid.NewGuid():N}@voltscrm.local";

        // Act
        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = newEmail,
            firstName = "New",
            lastName = "Admin",
            roleIds = new[] { admin.RoleId },
            isSuperAdmin = false
        }, token);

        // Assert: 201 Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await ReadAsync<AdminUserDtoT>(response);
        Assert.NotNull(dto);
        Assert.Equal(newEmail, dto!.Email);
        Assert.Equal("New Admin", dto.FullName);
        Assert.False(dto.IsSuperAdmin);
        Assert.Contains(admin.RoleId, dto.RoleIds);

        // Verify database state
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var createdUser = await db.Users.FirstOrDefaultAsync(u => u.Email == newEmail, Ct);
        Assert.NotNull(createdUser);
        Assert.Equal(UserType.Administration, createdUser!.UserType);
        Assert.True(createdUser.MustChangePassword);
        Assert.True(createdUser.EmailConfirmed);

        // Verify AspNetUserRoles entry for "Administrator" role exists
        var hasAdminRole = await db.UserRoles.AnyAsync(ur => ur.UserId == createdUser.Id, Ct);
        Assert.True(hasAdminRole);

        // Verify administration_users row
        var adminProfile = await db.AdministrationUsers
            .Include(a => a.Roles)
            .FirstOrDefaultAsync(a => a.UserId == createdUser.Id, Ct);
        Assert.NotNull(adminProfile);
        Assert.False(adminProfile!.IsSuperAdmin);
        Assert.Contains(adminProfile.Roles, r => r.AdminRoleId == admin.RoleId);

        // Verify invite email was sent
        Assert.Single(_fakeEmail.SentEmails);
        var email = _fakeEmail.SentEmails[0];
        Assert.Equal(newEmail, email.To);
        Assert.Contains("set-password", email.HtmlBody);
    }

    [Fact]
    public async Task CreateAdmin_NonSuperCallerWithSuperFlag_Returns403()
    {
        // Arrange: Create a non-super admin with access.manage
        using var scope = _factory.CreateScopeForArrange();
        var admin = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        // Act: Try to create a super admin
        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = $"newsuperadmin_{Guid.NewGuid():N}@voltscrm.local",
            firstName = "Super",
            lastName = "Admin",
            roleIds = Array.Empty<Guid>(),
            isSuperAdmin = true // This should be denied
        }, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_SuperCallerWithSuperFlag_Returns201WithSuperProfile()
    {
        // Arrange: Create a super admin with access.manage
        using var scope = _factory.CreateScopeForArrange();
        var superAdmin = await TestUsers.CreateSuperAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(superAdmin.UserId, UserType.Administration, [Permissions.AccessManage]);

        var newEmail = $"newsuperadmin_{Guid.NewGuid():N}@voltscrm.local";

        // Act
        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = newEmail,
            firstName = "New",
            lastName = "SuperAdmin",
            roleIds = Array.Empty<Guid>(),
            isSuperAdmin = true
        }, token);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await ReadAsync<AdminUserDtoT>(response);
        Assert.NotNull(dto);
        Assert.True(dto!.IsSuperAdmin);

        // Verify database state
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminProfile = await db.AdministrationUsers.FirstOrDefaultAsync(a => a.Id == dto.Id, Ct);
        Assert.NotNull(adminProfile);
        Assert.True(adminProfile!.IsSuperAdmin);
    }

    [Fact]
    public async Task CreateAdmin_DuplicateEmail_Returns409()
    {
        // Arrange
        using var scope = _factory.CreateScopeForArrange();
        var admin = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        // Create another user to use as the duplicate
        var existing = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [], "existing@voltscrm.local");

        // Act: Try to create an admin with the same email
        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = "existing@voltscrm.local",
            firstName = "Duplicate",
            lastName = "Admin",
            roleIds = Array.Empty<Guid>(),
            isSuperAdmin = false
        }, token);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_UnknownRoleId_Returns400()
    {
        // Arrange
        using var scope = _factory.CreateScopeForArrange();
        var admin = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        // Act: Try to create with a non-existent role ID
        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = $"newadmin_{Guid.NewGuid():N}@voltscrm.local",
            firstName = "New",
            lastName = "Admin",
            roleIds = new[] { Guid.NewGuid() }, // Non-existent role
            isSuperAdmin = false
        }, token);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_EmptyEmail_Returns400()
    {
        // Arrange
        using var scope = _factory.CreateScopeForArrange();
        var admin = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        // Act
        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = "",
            firstName = "New",
            lastName = "Admin",
            roleIds = Array.Empty<Guid>(),
            isSuperAdmin = false
        }, token);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_MissingAccessManagePermission_Returns403()
    {
        // Arrange: Create a token with no permissions
        var token = TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Administration, []);

        // Act
        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = $"newadmin_{Guid.NewGuid():N}@voltscrm.local",
            firstName = "New",
            lastName = "Admin",
            roleIds = Array.Empty<Guid>(),
            isSuperAdmin = false
        }, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_AssignsMultipleRoles_AllRolesInProfile()
    {
        // Arrange
        using var scope = _factory.CreateScopeForArrange();
        var admin = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var admin2 = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.InvoicesView]);
        var token = TestTokenFactory.Create(admin.UserId, UserType.Administration, [Permissions.AccessManage]);

        var newEmail = $"multirole_{Guid.NewGuid():N}@voltscrm.local";

        // Act
        var response = await PostAsync("/api/admin/access/admins", new
        {
            email = newEmail,
            firstName = "Multi",
            lastName = "Role",
            roleIds = new[] { admin.RoleId, admin2.RoleId },
            isSuperAdmin = false
        }, token);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await ReadAsync<AdminUserDtoT>(response);
        Assert.NotNull(dto);
        Assert.Contains(admin.RoleId, dto!.RoleIds);
        Assert.Contains(admin2.RoleId, dto.RoleIds);
    }
}

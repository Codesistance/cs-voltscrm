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
/// Integration tests for POST /api/admin/access/admins/{id}/reset-password — the no-email password
/// reset facility (admin supplies a password, or the server generates one and returns it once).
/// </summary>
[Collection("SharedTestContainers")]
public class ResetPasswordTests : IAsyncLifetime
{
    private readonly SharedTestContainersFixture _fixture;
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public ResetPasswordTests(SharedTestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory(
            _fixture.PostgresConnectionString, nameof(ResetPasswordTests), new FakeEmailSender());
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

    private static Task<T?> ReadAsync<T>(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<T>(Ct);

    [Fact]
    public async Task ResetPassword_NoNewPassword_GeneratesOneThatLogsInAndForcesChange()
    {
        using var scope = _factory.CreateScopeForArrange();
        var caller = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var target = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, []);
        var callerToken = TestTokenFactory.Create(caller.UserId, UserType.Administration, [Permissions.AccessManage]);

        var response = await PostAsync(
            $"/api/admin/access/admins/{target.AdminProfileId}/reset-password", new { }, callerToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync<ResetPasswordResultDtoT>(response);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.TemporaryPassword));

        // Old password no longer works.
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var targetUser = await db.Users.FirstAsync(u => u.Id == target.UserId, Ct);
        var oldLogin = await PostAsync("/api/auth/login",
            new { email = targetUser.Email, password = TestUsers.DefaultPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        // Generated password logs in and reports MustChangePassword = true.
        var newLogin = await PostAsync("/api/auth/login",
            new { email = targetUser.Email, password = result.TemporaryPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        var loginBody = await ReadAsync<LoginResponseDto>(newLogin);
        Assert.NotNull(loginBody);
        Assert.True(loginBody!.User.MustChangePassword);
    }

    [Fact]
    public async Task ResetPassword_WithNewPassword_SetsItAndReturnsNoTemporaryPassword()
    {
        using var scope = _factory.CreateScopeForArrange();
        var caller = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var target = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, []);
        var callerToken = TestTokenFactory.Create(caller.UserId, UserType.Administration, [Permissions.AccessManage]);

        const string chosenPassword = "Chosen123!pass";
        var response = await PostAsync(
            $"/api/admin/access/admins/{target.AdminProfileId}/reset-password",
            new { newPassword = chosenPassword }, callerToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync<ResetPasswordResultDtoT>(response);
        Assert.NotNull(result);
        Assert.Null(result!.TemporaryPassword);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var targetUser = await db.Users.FirstAsync(u => u.Id == target.UserId, Ct);
        var login = await PostAsync("/api/auth/login", new { email = targetUser.Email, password = chosenPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await ReadAsync<LoginResponseDto>(login);
        Assert.True(loginBody!.User.MustChangePassword);
    }

    [Fact]
    public async Task ResetPassword_MissingAccessManagePermission_Returns403()
    {
        using var scope = _factory.CreateScopeForArrange();
        var target = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, []);
        var token = TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Administration, []);

        var response = await PostAsync(
            $"/api/admin/access/admins/{target.AdminProfileId}/reset-password", new { }, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UnknownAdminId_Returns404()
    {
        using var scope = _factory.CreateScopeForArrange();
        var caller = await TestUsers.CreateAdminWithRoleAsync(scope.ServiceProvider, [Permissions.AccessManage]);
        var callerToken = TestTokenFactory.Create(caller.UserId, UserType.Administration, [Permissions.AccessManage]);

        var response = await PostAsync(
            $"/api/admin/access/admins/{Guid.NewGuid()}/reset-password", new { }, callerToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

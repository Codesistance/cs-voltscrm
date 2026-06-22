using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

[Collection("SharedTestContainers")]
public class AuthFlowTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(AuthFlowTests);

    private const string KnownPassword = "KnownAdmin123!pass";

    /// <summary>
    /// Sets the seeded admin's password to a known value (the production seeder rotates it daily, which
    /// is brittle to recompute in a test). Exercises the real login pipeline against a real credential.
    /// </summary>
    private async Task SetSeededAdminPasswordAsync(string password)
    {
        using var scope = Factory.CreateScopeForArrange();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var admin = await userManager.FindByEmailAsync(SeedCredentialGenerator.SeededAdminEmail);
        Assert.NotNull(admin);
        var token = await userManager.GeneratePasswordResetTokenAsync(admin!);
        var result = await userManager.ResetPasswordAsync(admin!, token, password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private object SeededAdminCredentials() => new
    {
        email = SeedCredentialGenerator.SeededAdminEmail,
        password = KnownPassword,
    };

    [Fact]
    public async Task Login_SeededAdmin_ReturnsAdministrationTypeAndPermissions()
    {
        await SetSeededAdminPasswordAsync(KnownPassword);

        var response = await PostAsync("/api/auth/login", SeededAdminCredentials());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadAsync<LoginResponseDto>(response);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.Equal("Administration", body.User.UserType);
        Assert.Contains(Permissions.AccessManage, body.User.Permissions);
    }

    [Fact]
    public async Task Me_WithSeededAdminToken_EchoesUserTypeAndPermissions()
    {
        await SetSeededAdminPasswordAsync(KnownPassword);

        var login = await PostAsync("/api/auth/login", SeededAdminCredentials());
        var token = (await ReadAsync<LoginResponseDto>(login))!.AccessToken;

        var response = await GetAsync("/api/auth/me", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await ReadAsync<AuthUserDtoT>(response);
        Assert.NotNull(me);
        Assert.Equal("Administration", me!.UserType);
        Assert.Contains(Permissions.AccessManage, me.Permissions);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var response = await PostAsync("/api/auth/login", new
        {
            email = SeedCredentialGenerator.SeededAdminEmail,
            password = "definitely-not-the-password",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

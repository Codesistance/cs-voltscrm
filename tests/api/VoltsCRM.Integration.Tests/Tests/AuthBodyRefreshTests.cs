using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Exercises the cookie-less refresh path (Auth:RefreshTokenInBody = true) used by the
/// no-custom-domain edge, where the SPA and API are cross-origin over plain HTTP and a Secure
/// cookie can't be carried. Login returns the refresh token in the body; refresh replays it in the
/// body and rotates it; the old token is then rejected.
/// </summary>
[Collection("SharedTestContainers")]
public class AuthBodyRefreshTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(AuthBodyRefreshTests);

    protected override IReadOnlyDictionary<string, string?> ExtraConfig =>
        new Dictionary<string, string?> { ["Auth:RefreshTokenInBody"] = "true" };

    private const string KnownPassword = "KnownAdmin123!pass";

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
    public async Task Login_InBodyMode_ReturnsRefreshTokenInBody()
    {
        await SetSeededAdminPasswordAsync(KnownPassword);

        var response = await PostAsync("/api/auth/login", SeededAdminCredentials());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadAsync<LoginResponseDto>(response);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.RefreshToken));
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Refresh_WithBodyToken_RotatesAndInvalidatesOldToken()
    {
        await SetSeededAdminPasswordAsync(KnownPassword);

        var login = await PostAsync("/api/auth/login", SeededAdminCredentials());
        var first = (await ReadAsync<LoginResponseDto>(login))!.RefreshToken;
        Assert.False(string.IsNullOrEmpty(first));

        // Replay the refresh token in the body — succeeds and rotates to a new token.
        var refresh = await PostAsync("/api/auth/refresh", new { refreshToken = first });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotated = await ReadAsync<RefreshResponseDto>(refresh);
        Assert.NotNull(rotated);
        Assert.False(string.IsNullOrEmpty(rotated!.AccessToken));
        Assert.False(string.IsNullOrEmpty(rotated.RefreshToken));
        Assert.NotEqual(first, rotated.RefreshToken);

        // The rotated (new) token works.
        var again = await PostAsync("/api/auth/refresh", new { refreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        // The original token is now revoked.
        var reused = await PostAsync("/api/auth/refresh", new { refreshToken = first });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task Refresh_MissingBodyToken_ReturnsUnauthorized()
    {
        var response = await PostAsync("/api/auth/refresh", new { refreshToken = (string?)null });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

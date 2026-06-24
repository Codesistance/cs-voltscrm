using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VoltsCRM.API.Setup;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests;

/// <summary>
/// Boots the VoltsCRM API against an isolated database inside the shared PostgreSQL container.
/// Mirrors Concairge's <c>CustomWebApplicationFactory</c>: overrides config to point at the test DB,
/// runs in the "Testing" environment, then applies the production seeding via
/// <see cref="DbSeeder.SeedAsync"/>. JWT validation is left intact (issuer/audience/lifetime/key) so
/// tests exercise the real auth pipeline.
/// </summary>
public sealed class CustomWebApplicationFactory(
    string masterConnectionString,
    string? testName = null,
    FakeEmailSender? fakeEmailSender = null,
    IReadOnlyDictionary<string, string?>? extraConfig = null)
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Test signing key — must be ≥32 chars to satisfy the startup guard in Program.cs.</summary>
    public const string TestJwtKey = "voltscrm-integration-test-signing-key-0123456789";

    /// <summary>Test seed HMAC key — must be ≥32 chars to satisfy the Seed:HmacKey startup guard.</summary>
    public const string TestSeedHmacKey = "voltscrm-integration-test-seed-hmac-key-0123456789";

    /// <summary>Test webhook secret for the voltspayments gateway (injected into its config row by the seeder).</summary>
    public const string TestVoltspaymentsWebhookSecret = "voltscrm-integration-test-voltspayments-webhook-secret";

    public const string JwtIssuer = "VoltsCRM";
    public const string JwtAudience = "VoltsCRM";

    private readonly TestDatabaseHelper _databaseHelper = new(masterConnectionString, testName);

    /// <summary>The FakeEmailSender instance if one was provided, allowing tests to assert on sent emails.</summary>
    public FakeEmailSender? FakeEmailSender => fakeEmailSender;

    public string ConnectionString => _databaseHelper.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Per-factory config overrides (e.g. Auth:RefreshTokenInBody). Safe via in-memory source
        // because these are read at request time (IOptions), not during builder setup like Jwt:Key.
        if (extraConfig is { Count: > 0 })
            builder.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(extraConfig));

        if (fakeEmailSender is not null)
        {
            builder.ConfigureServices(services =>
            {
                // Replace the default IEmailSender with our fake
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(fakeEmailSender);
            });
        }

        base.ConfigureWebHost(builder);
    }

    public async ValueTask InitializeAsync()
    {
        // 1. Create + migrate the isolated DB (schema + seeded admin user) before the host starts.
        await _databaseHelper.CreateDatabaseAsync();

        // 2. Supply config via environment variables. Program.cs reads Jwt:Key (and the connection
        //    string) during builder setup — before WebApplicationFactory's ConfigureAppConfiguration
        //    callbacks run — so env vars (a default CreateBuilder source) are the reliable channel.
        //    All test classes share one collection, so these run sequentially without clobbering.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _databaseHelper.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("Seed__HmacKey", TestSeedHmacKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "1000");
        Environment.SetEnvironmentVariable("Payments__Voltspayments__WebhookSecret", TestVoltspaymentsWebhookSecret);

        // 3. Run production seeding (permission catalogue, super-admin role perms, admin password)
        //    using the app's DI. Accessing Services builds the host against the env config above.
        using var scope = Services.CreateScope();
        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }

    /// <summary>Opens a service scope for arrange-phase data setup.</summary>
    public IServiceScope CreateScopeForArrange() => Services.CreateScope();

    public new async ValueTask DisposeAsync()
    {
        await _databaseHelper.DisposeAsync();
        await base.DisposeAsync();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;

namespace VoltsCRM.API.Setup;

public static class DbSeeder
{
    public static readonly string[] Roles = ["Administrator", "Agent", "Customer"];

    /// <summary>Applies pending migrations, then runs <see cref="SeedAsync"/>.</summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await SeedAsync(sp);
    }

    /// <summary>
    /// Drops the database, re-applies all migrations from scratch, then seeds.
    /// DESTRUCTIVE: destroys all data. Dev/local use only.
    /// </summary>
    public static async Task ReinitializeAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await SeedAsync(sp);
    }

    /// <summary>
    /// Seeds the identity roles and the permission catalogue (required so admin roles can reference
    /// permission keys), then rotates the seeded admin's password to today's computed value. No demo
    /// data is created — only the admin user. Assumes migrations have already been applied (the admin
    /// user and the Super Administrator role are created by the SeedAdminUser migration).
    /// Reused by both the app startup path and the integration-test harness.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var db = sp.GetRequiredService<AppDbContext>();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await SeedPermissionCatalogueAsync(db);
        await UpdateSeededAdminPasswordAsync(sp, logger);
        await InjectGatewayWebhookSecretsAsync(sp, db);
    }

    /// <summary>
    /// Injects gateway webhook secrets from configuration into the gateway config rows at startup, so
    /// secrets are never baked into source/migrations (mirrors the Seed:HmacKey approach). Currently
    /// the first-party "voltspayments" gateway, keyed by Payments:Voltspayments:WebhookSecret.
    /// </summary>
    private static async Task InjectGatewayWebhookSecretsAsync(IServiceProvider sp, AppDbContext db)
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var secret = config["Payments:Voltspayments:WebhookSecret"];
        if (string.IsNullOrEmpty(secret))
            return;

        var row = await db.PaymentGatewayConfigs.FirstOrDefaultAsync(c => c.KeyName == "voltspayments");
        if (row is null)
            return;

        if (!row.Data.TryGetValue("webhookSecret", out var existing) || existing != secret)
        {
            var data = new Dictionary<string, string>(row.Data) { ["webhookSecret"] = secret };
            row.Update(row.DisplayName, row.Visibility, data);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Upserts the code-defined permission catalogue into the database.</summary>
    private static async Task SeedPermissionCatalogueAsync(AppDbContext db)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Key);
        var changed = false;

        foreach (var def in Permissions.All)
        {
            if (existing.TryGetValue(def.Key, out var row))
            {
                if (row.Description != def.Description || row.Group != def.Group)
                {
                    row.Description = def.Description;
                    row.Group = def.Group;
                    changed = true;
                }
            }
            else
            {
                db.Permissions.Add(new Permission { Key = def.Key, Description = def.Description, Group = def.Group });
                changed = true;
            }
        }

        if (changed) await db.SaveChangesAsync();
    }

    /// <summary>
    /// Updates the seeded admin's password to today's computed value.
    /// The admin user is created by migration; this ensures the password rotates daily.
    /// </summary>
    private static async Task UpdateSeededAdminPasswordAsync(IServiceProvider sp, ILogger logger)
    {
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var admin = await userManager.FindByEmailAsync(SeedCredentialGenerator.SeededAdminEmail);

        if (admin is null)
        {
            logger.LogWarning("Seeded admin user not found. Run migrations to create it.");
            return;
        }

        // Compute today's password from the configured secret HMAC key and update.
        var hmacKey = sp.GetRequiredService<IOptions<SeedOptions>>().Value.HmacKey;
        var todaysPassword = SeedCredentialGenerator.ComputeTodaysPassword(hmacKey);
        var token = await userManager.GeneratePasswordResetTokenAsync(admin);
        var result = await userManager.ResetPasswordAsync(admin, token, todaysPassword);

        if (result.Succeeded)
        {
            logger.LogInformation("Seeded admin password updated for {Date}", DateTime.UtcNow.ToString("dd/MM/yyyy"));
        }
        else
        {
            logger.LogError("Failed to update seeded admin password: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}

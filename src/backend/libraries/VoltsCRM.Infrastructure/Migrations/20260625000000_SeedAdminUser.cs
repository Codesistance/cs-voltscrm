using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;

#nullable disable

namespace VoltsCRM.Infrastructure.Migrations;

/// <summary>
/// Seeds the initial admin user with a computed password.
/// The password is updated on every application startup to match the current date.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260625000000_SeedAdminUser")]
public partial class SeedAdminUser : Migration
{
    // Fixed IDs for seeded data - allows idempotent operations and foreign key references
    private static readonly string SeededAdminUserId = "00000000-0000-0000-0000-000000000001";
    private static readonly Guid SeededAdminProfileId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid SuperAdminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly string AdministratorRoleId = "00000000-0000-0000-0000-000000000004";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var now = DateTimeOffset.UtcNow;
        // The initial password is a random, unusable placeholder. The real (key-derived) password is
        // set on every application startup by DbSeeder.UpdateSeededAdminPasswordAsync. A migration has
        // no access to the secret HMAC key and must never bake a deterministic credential into the DB.
        var passwordHash = new PasswordHasher<AppUser>().HashPassword(null!, Guid.NewGuid().ToString());
        var email = SeedCredentialGenerator.SeededAdminEmail;
        var normalizedEmail = email.ToUpperInvariant();

        // Every statement is idempotent on the NATURAL key (not the PK), so it coexists with the
        // runtime DbSeeder (which uses random GUIDs): if the row already exists, "if exists" wins and
        // we skip. Junction rows resolve the real parent ids by lookup, so they work regardless of
        // which path created the parent.

        // 1. Administrator role (ASP.NET Identity) — skip if a role with this name already exists.
        migrationBuilder.Sql($"""
            INSERT INTO identity."AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
            SELECT '{AdministratorRoleId}', 'Administrator', 'ADMINISTRATOR', '{Guid.NewGuid()}'
            WHERE NOT EXISTS (
                SELECT 1 FROM identity."AspNetRoles" WHERE "NormalizedName" = 'ADMINISTRATOR'
            );
            """);

        // 2. Super Administrator admin role (custom RBAC) — skip if it already exists by name.
        migrationBuilder.Sql($"""
            INSERT INTO identity.admin_roles ("Id", "Name", "Description", "IsSystem", "CreatedAt")
            SELECT '{SuperAdminRoleId}', 'Super Administrator', 'Full, unrestricted access. Cannot be deleted.', true, '{now:O}'
            WHERE NOT EXISTS (
                SELECT 1 FROM identity.admin_roles WHERE "Name" = 'Super Administrator'
            );
            """);

        // 3. Seeded admin user — skip if a user with this normalized username already exists.
        migrationBuilder.Sql($"""
            INSERT INTO identity."AspNetUsers" (
                "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
                "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
                "FirstName", "LastName", "UserType", "IsActive", "CreatedAt"
            )
            SELECT
                '{SeededAdminUserId}', '{email}', '{normalizedEmail}', '{email}', '{normalizedEmail}',
                true, '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}',
                false, false, true, 0,
                'System', 'Administrator', 'Administration', true, '{now:O}'
            WHERE NOT EXISTS (
                SELECT 1 FROM identity."AspNetUsers" WHERE "NormalizedUserName" = '{normalizedEmail}'
            );
            """);

        // 4. Assign the Administrator role to the admin — resolve the actual user/role ids.
        migrationBuilder.Sql($"""
            INSERT INTO identity."AspNetUserRoles" ("UserId", "RoleId")
            SELECT u."Id", r."Id"
            FROM identity."AspNetUsers" u
            CROSS JOIN identity."AspNetRoles" r
            WHERE u."NormalizedUserName" = '{normalizedEmail}'
              AND r."NormalizedName" = 'ADMINISTRATOR'
              AND NOT EXISTS (
                  SELECT 1 FROM identity."AspNetUserRoles" ur
                  WHERE ur."UserId" = u."Id" AND ur."RoleId" = r."Id"
              );
            """);

        // 5. AdministrationUser profile — resolve the user id; skip if a profile already exists for it.
        migrationBuilder.Sql($"""
            INSERT INTO identity.administration_users ("Id", "UserId", "IsSuperAdmin", "CreatedAt")
            SELECT '{SeededAdminProfileId}', u."Id", true, '{now:O}'
            FROM identity."AspNetUsers" u
            WHERE u."NormalizedUserName" = '{normalizedEmail}'
              AND NOT EXISTS (
                  SELECT 1 FROM identity.administration_users a WHERE a."UserId" = u."Id"
              );
            """);

        // 6. Link the profile to the Super Administrator role — resolve the actual ids.
        migrationBuilder.Sql($"""
            INSERT INTO identity.admin_user_roles ("AdministrationUserId", "AdminRoleId")
            SELECT a."Id", ar."Id"
            FROM identity.administration_users a
            JOIN identity."AspNetUsers" u ON u."Id" = a."UserId"
            CROSS JOIN identity.admin_roles ar
            WHERE u."NormalizedUserName" = '{normalizedEmail}'
              AND ar."Name" = 'Super Administrator'
              AND NOT EXISTS (
                  SELECT 1 FROM identity.admin_user_roles aur
                  WHERE aur."AdministrationUserId" = a."Id" AND aur."AdminRoleId" = ar."Id"
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove in reverse order of dependencies
        migrationBuilder.Sql($"DELETE FROM identity.admin_user_roles WHERE \"AdministrationUserId\" = '{SeededAdminProfileId}';");
        migrationBuilder.Sql($"DELETE FROM identity.administration_users WHERE \"Id\" = '{SeededAdminProfileId}';");
        migrationBuilder.Sql($"DELETE FROM identity.\"AspNetUserRoles\" WHERE \"UserId\" = '{SeededAdminUserId}';");
        migrationBuilder.Sql($"DELETE FROM identity.\"AspNetUsers\" WHERE \"Id\" = '{SeededAdminUserId}';");
        migrationBuilder.Sql($"DELETE FROM identity.admin_roles WHERE \"Id\" = '{SuperAdminRoleId}';");
        migrationBuilder.Sql($"DELETE FROM identity.\"AspNetRoles\" WHERE \"Id\" = '{AdministratorRoleId}';");
    }
}

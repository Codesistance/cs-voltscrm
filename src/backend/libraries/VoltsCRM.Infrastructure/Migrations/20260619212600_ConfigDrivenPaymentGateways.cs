using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltsCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigDrivenPaymentGateways : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_gateway_settings",
                schema: "organisation");

            migrationBuilder.CreateTable(
                name: "payment_gateway_configs",
                schema: "organisation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Visibility = table.Column<bool>(type: "boolean", nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_gateway_configs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_gateway_configs_KeyName",
                schema: "organisation",
                table: "payment_gateway_configs",
                column: "KeyName",
                unique: true);

            // Seed the first-party no-op gateway, visible by default. Idempotent on the natural key
            // (KeyName). The webhookSecret is intentionally NOT baked here — DbSeeder injects it at
            // startup from configuration (Payments:Voltspayments:WebhookSecret), like the seed HMAC key.
            var now = DateTimeOffset.UtcNow;
            var emptyJson = "'{}'::jsonb"; // braces kept out of the interpolated raw string below
            migrationBuilder.Sql($"""
                INSERT INTO organisation.payment_gateway_configs
                    ("Id", "KeyName", "DisplayName", "Visibility", "Data", "CreatedAt", "UpdatedAt")
                SELECT '{Guid.NewGuid()}', 'voltspayments', 'Volts Payments', true, {emptyJson}, '{now:O}', '{now:O}'
                WHERE NOT EXISTS (
                    SELECT 1 FROM organisation.payment_gateway_configs WHERE "KeyName" = 'voltspayments'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_gateway_configs",
                schema: "organisation");

            migrationBuilder.CreateTable(
                name: "payment_gateway_settings",
                schema: "organisation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MerchantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PublicKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_gateway_settings", x => x.Id);
                });
        }
    }
}

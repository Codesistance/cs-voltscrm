using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltsCRM.Infrastructure.Persistence;

#nullable disable

namespace VoltsCRM.Infrastructure.Migrations;

/// <summary>Creates the append-only <c>audit_events</c> table backing the security audit trail.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260824220000_AddAuditEvents")]
public partial class AddAuditEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "audit_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                ActorUserId = table.Column<string>(type: "text", nullable: true),
                ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                TargetType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                TargetId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                TargetLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                Details = table.Column<string>(type: "jsonb", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_events", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_audit_events_Action",
            table: "audit_events",
            column: "Action");

        migrationBuilder.CreateIndex(
            name: "IX_audit_events_ActorEmail",
            table: "audit_events",
            column: "ActorEmail");

        migrationBuilder.CreateIndex(
            name: "IX_audit_events_OccurredAt",
            table: "audit_events",
            column: "OccurredAt");

        migrationBuilder.CreateIndex(
            name: "IX_audit_events_TargetId",
            table: "audit_events",
            column: "TargetId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audit_events");
    }
}

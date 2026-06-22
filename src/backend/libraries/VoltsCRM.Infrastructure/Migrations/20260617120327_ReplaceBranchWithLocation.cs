using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltsCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBranchWithLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branches",
                schema: "crm");

            migrationBuilder.DropColumn(
                name: "BranchId",
                schema: "crm",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                schema: "crm",
                table: "agents");

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "crm",
                table: "agents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "country",
                schema: "crm",
                table: "agents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                schema: "crm",
                table: "agents",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                schema: "crm",
                table: "agents",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "crm",
                table: "agents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "street",
                schema: "crm",
                table: "agents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "city",
                schema: "crm",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "country",
                schema: "crm",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "latitude",
                schema: "crm",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "longitude",
                schema: "crm",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "crm",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "street",
                schema: "crm",
                table: "agents");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                schema: "crm",
                table: "customers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                schema: "crm",
                table: "agents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.Id);
                });
        }
    }
}

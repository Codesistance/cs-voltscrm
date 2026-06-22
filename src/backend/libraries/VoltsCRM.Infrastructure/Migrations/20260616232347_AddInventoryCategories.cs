using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltsCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "inventory",
                table: "inventory_items");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                schema: "inventory",
                table: "inventory_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "inventory_categories",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    TracksStock = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_CategoryId",
                schema: "inventory",
                table: "inventory_items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_categories_Name",
                schema: "inventory",
                table: "inventory_categories",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_items_inventory_categories_CategoryId",
                schema: "inventory",
                table: "inventory_items",
                column: "CategoryId",
                principalSchema: "inventory",
                principalTable: "inventory_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_items_inventory_categories_CategoryId",
                schema: "inventory",
                table: "inventory_items");

            migrationBuilder.DropTable(
                name: "inventory_categories",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_inventory_items_CategoryId",
                schema: "inventory",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "inventory",
                table: "inventory_items");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "inventory",
                table: "inventory_items",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}

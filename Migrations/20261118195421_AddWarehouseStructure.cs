using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Area",
                table: "Warehouses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarehouseEmployees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsResponsible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseEmployees_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseEmployees_WarehouseId",
                table: "WarehouseEmployees",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseEmployees");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WorkingHours",
                table: "Warehouses");
        }
    }
}

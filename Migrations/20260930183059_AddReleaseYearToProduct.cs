using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseYearToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReleaseYear",
                table: "Products",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReleaseYear",
                table: "Products");
        }
    }
}

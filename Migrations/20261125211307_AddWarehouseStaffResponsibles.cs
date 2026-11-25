using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseStaffResponsibles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SourceResponsibleEmployeeId",
                table: "WarehouseTransfer",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DestinationResponsibleEmployeeId",
                table: "WarehouseTransfer",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "DestinationResponsibleUserId",
                table: "WarehouseTransfer",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceResponsibleUserId",
                table: "WarehouseTransfer",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWarehouseResponsible",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfer_DestinationResponsibleUserId",
                table: "WarehouseTransfer",
                column: "DestinationResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfer_SourceResponsibleUserId",
                table: "WarehouseTransfer",
                column: "SourceResponsibleUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransfer_Users_DestinationResponsibleUserId",
                table: "WarehouseTransfer",
                column: "DestinationResponsibleUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseTransfer_Users_SourceResponsibleUserId",
                table: "WarehouseTransfer",
                column: "SourceResponsibleUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransfer_Users_DestinationResponsibleUserId",
                table: "WarehouseTransfer");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseTransfer_Users_SourceResponsibleUserId",
                table: "WarehouseTransfer");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseTransfer_DestinationResponsibleUserId",
                table: "WarehouseTransfer");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseTransfer_SourceResponsibleUserId",
                table: "WarehouseTransfer");

            migrationBuilder.DropColumn(
                name: "DestinationResponsibleUserId",
                table: "WarehouseTransfer");

            migrationBuilder.DropColumn(
                name: "SourceResponsibleUserId",
                table: "WarehouseTransfer");

            migrationBuilder.DropColumn(
                name: "IsWarehouseResponsible",
                table: "Users");

            migrationBuilder.AlterColumn<int>(
                name: "SourceResponsibleEmployeeId",
                table: "WarehouseTransfer",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DestinationResponsibleEmployeeId",
                table: "WarehouseTransfer",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}

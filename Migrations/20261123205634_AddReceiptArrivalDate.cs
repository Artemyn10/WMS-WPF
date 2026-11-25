using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptArrivalDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ArrivalDate",
                table: "Receipts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_CreatedByUserId",
                table: "Receipts",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Users_CreatedByUserId",
                table: "Receipts",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Users_CreatedByUserId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_CreatedByUserId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ArrivalDate",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Receipts");
        }
    }
}

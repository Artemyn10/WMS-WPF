using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WMS.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseTransfer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceWarehouseId = table.Column<int>(type: "integer", nullable: false),
                    DestinationWarehouseId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SourceResponsibleEmployeeId = table.Column<int>(type: "integer", nullable: false),
                    DestinationResponsibleEmployeeId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransfer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfer_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfer_WarehouseEmployees_DestinationResponsible~",
                        column: x => x.DestinationResponsibleEmployeeId,
                        principalTable: "WarehouseEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfer_WarehouseEmployees_SourceResponsibleEmplo~",
                        column: x => x.SourceResponsibleEmployeeId,
                        principalTable: "WarehouseEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfer_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfer_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransferItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseTransferId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SourceStorageLocationId = table.Column<int>(type: "integer", nullable: false),
                    DestinationStorageLocationId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransferItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferItem_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferItem_StorageLocations_DestinationStorageLo~",
                        column: x => x.DestinationStorageLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferItem_StorageLocations_SourceStorageLocatio~",
                        column: x => x.SourceStorageLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferItem_WarehouseTransfer_WarehouseTransferId",
                        column: x => x.WarehouseTransferId,
                        principalTable: "WarehouseTransfer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfer_CreatedByUserId",
                table: "WarehouseTransfer",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfer_DestinationResponsibleEmployeeId",
                table: "WarehouseTransfer",
                column: "DestinationResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfer_DestinationWarehouseId",
                table: "WarehouseTransfer",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfer_DocumentNumber",
                table: "WarehouseTransfer",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfer_SourceResponsibleEmployeeId",
                table: "WarehouseTransfer",
                column: "SourceResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfer_SourceWarehouseId",
                table: "WarehouseTransfer",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferItem_DestinationStorageLocationId",
                table: "WarehouseTransferItem",
                column: "DestinationStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferItem_ProductId",
                table: "WarehouseTransferItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferItem_SourceStorageLocationId",
                table: "WarehouseTransferItem",
                column: "SourceStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferItem_WarehouseTransferId",
                table: "WarehouseTransferItem",
                column: "WarehouseTransferId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseTransferItem");

            migrationBuilder.DropTable(
                name: "WarehouseTransfer");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItemMediaMVP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItemVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SkuSuffix = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PriceAdjustment = table.Column<decimal>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemVariants_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VariantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    AltText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsMain = table.Column<bool>(type: "INTEGER", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemImages_InventoryItemVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "InventoryItemVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItemImages_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemVariantAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VariantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttributeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AttributeValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemVariantAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemVariantAttributes_InventoryItemVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "InventoryItemVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemImages_InventoryItemId",
                table: "InventoryItemImages",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemImages_VariantId",
                table: "InventoryItemImages",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemVariantAttributes_VariantId_AttributeName",
                table: "InventoryItemVariantAttributes",
                columns: new[] { "VariantId", "AttributeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemVariants_InventoryItemId_Name",
                table: "InventoryItemVariants",
                columns: new[] { "InventoryItemId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItemImages");

            migrationBuilder.DropTable(
                name: "InventoryItemVariantAttributes");

            migrationBuilder.DropTable(
                name: "InventoryItemVariants");
        }
    }
}

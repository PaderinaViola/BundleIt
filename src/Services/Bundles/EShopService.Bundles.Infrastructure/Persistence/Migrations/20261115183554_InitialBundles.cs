using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopService.Bundles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBundles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bundles");

            migrationBuilder.CreateTable(
                name: "Bundles",
                schema: "bundles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrimaryArtistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PackLicenseType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PersonalPriceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PersonalPriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CommercialPriceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CommercialPriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CoopArtistIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BundleItems",
                schema: "bundles",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    License = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsReported = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleItems", x => new { x.BundleId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_BundleItems_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "bundles",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_PrimaryArtistId",
                schema: "bundles",
                table: "Bundles",
                column: "PrimaryArtistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BundleItems",
                schema: "bundles");

            migrationBuilder.DropTable(
                name: "Bundles",
                schema: "bundles");
        }
    }
}

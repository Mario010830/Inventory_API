using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductStockParentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockParentProductId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StockUnitsConsumedPerSaleUnit",
                table: "Products",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_StockParentProductId",
                table: "Products",
                column: "StockParentProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Products_StockParentProductId",
                table: "Products",
                column: "StockParentProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Products_StockParentProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StockParentProductId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockParentProductId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockUnitsConsumedPerSaleUnit",
                table: "Products");
        }
    }
}

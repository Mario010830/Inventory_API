using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyDenominationsAndMulticurrencyPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountForeign",
                table: "SaleOrderPayments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "SaleOrderPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateSnapshot",
                table: "SaleOrderPayments",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurrencyDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrencyId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyDenominations_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleOrderPaymentDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaleOrderPaymentId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleOrderPaymentDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleOrderPaymentDenominations_SaleOrderPayments_SaleOrderPa~",
                        column: x => x.SaleOrderPaymentId,
                        principalTable: "SaleOrderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderPayments_CurrencyId",
                table: "SaleOrderPayments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyDenominations_CurrencyId_Value",
                table: "CurrencyDenominations",
                columns: new[] { "CurrencyId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderPaymentDenominations_SaleOrderPaymentId",
                table: "SaleOrderPaymentDenominations",
                column: "SaleOrderPaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrderPayments_Currencies_CurrencyId",
                table: "SaleOrderPayments",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrderPayments_Currencies_CurrencyId",
                table: "SaleOrderPayments");

            migrationBuilder.DropTable(
                name: "CurrencyDenominations");

            migrationBuilder.DropTable(
                name: "SaleOrderPaymentDenominations");

            migrationBuilder.DropIndex(
                name: "IX_SaleOrderPayments_CurrencyId",
                table: "SaleOrderPayments");

            migrationBuilder.DropColumn(
                name: "AmountForeign",
                table: "SaleOrderPayments");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SaleOrderPayments");

            migrationBuilder.DropColumn(
                name: "ExchangeRateSnapshot",
                table: "SaleOrderPayments");
        }
    }
}

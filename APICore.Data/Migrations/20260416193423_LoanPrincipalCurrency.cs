using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class LoanPrincipalCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrincipalCurrencyId",
                table: "Loans",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_PrincipalCurrencyId",
                table: "Loans",
                column: "PrincipalCurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_Currencies_PrincipalCurrencyId",
                table: "Loans",
                column: "PrincipalCurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Loans_Currencies_PrincipalCurrencyId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_PrincipalCurrencyId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "PrincipalCurrencyId",
                table: "Loans");
        }
    }
}

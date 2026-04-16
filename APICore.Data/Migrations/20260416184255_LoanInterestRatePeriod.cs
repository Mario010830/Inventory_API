using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class LoanInterestRatePeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InterestPercentPerYear",
                table: "Loans",
                newName: "InterestPercent");

            migrationBuilder.AddColumn<string>(
                name: "InterestRatePeriod",
                table: "Loans",
                type: "text",
                nullable: false,
                defaultValue: "annual");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterestRatePeriod",
                table: "Loans");

            migrationBuilder.RenameColumn(
                name: "InterestPercent",
                table: "Loans",
                newName: "InterestPercentPerYear");
        }
    }
}

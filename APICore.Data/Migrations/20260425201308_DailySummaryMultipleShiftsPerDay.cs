using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class DailySummaryMultipleShiftsPerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailySummaries_OrganizationId_LocationId_Date",
                table: "DailySummaries");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "DailySummaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStart",
                table: "DailySummaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "DailySummaries" SET "PeriodStart" = "Date" WHERE "PeriodStart" IS NULL;
                UPDATE "DailySummaries" SET "ClosedAt" = "ModifiedAt" WHERE "IsClosed" AND "ClosedAt" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PeriodStart",
                table: "DailySummaries",
                type: "timestamp with time zone",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_OrganizationId_LocationId_Date",
                table: "DailySummaries",
                columns: new[] { "OrganizationId", "LocationId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailySummaries_OrganizationId_LocationId_Date",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "DailySummaries");

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_OrganizationId_LocationId_Date",
                table: "DailySummaries",
                columns: new[] { "OrganizationId", "LocationId", "Date" },
                unique: true);
        }
    }
}

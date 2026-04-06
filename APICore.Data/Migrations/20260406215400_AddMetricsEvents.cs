using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricsEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetricsEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: true),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    SaleOrderId = table.Column<int>(type: "integer", nullable: true),
                    TrafficSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SearchTerm = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricsEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricsEvents_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricsEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricsEvents_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricsEvents_SaleOrders_SaleOrderId",
                        column: x => x.SaleOrderId,
                        principalTable: "SaleOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricsEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricsEvents_EventType",
                table: "MetricsEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_MetricsEvents_LocationId",
                table: "MetricsEvents",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricsEvents_OrganizationId_EventType_OccurredAt",
                table: "MetricsEvents",
                columns: new[] { "OrganizationId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricsEvents_OrganizationId_OccurredAt",
                table: "MetricsEvents",
                columns: new[] { "OrganizationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricsEvents_ProductId",
                table: "MetricsEvents",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricsEvents_SaleOrderId",
                table: "MetricsEvents",
                column: "SaleOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricsEvents_UserId",
                table: "MetricsEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetricsEvents");
        }
    }
}

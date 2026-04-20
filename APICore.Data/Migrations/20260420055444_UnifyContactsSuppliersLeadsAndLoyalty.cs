using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnifyContactsSuppliersLeadsAndLoyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MobileListLayout",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomer",
                table: "Contacts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSupplier",
                table: "Contacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeadConvertedAt",
                table: "Contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadStatus",
                table: "Contacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierContactId",
                table: "InventoryMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
ALTER TABLE ""Contacts"" ADD COLUMN IF NOT EXISTS ""MigrationSupplierId"" integer NULL;

INSERT INTO ""Contacts"" (""OrganizationId"",""Name"",""Company"",""ContactPerson"",""Phone"",""Email"",""Address"",""Notes"",""Origin"",""IsActive"",""AssignedUserId"",""CreatedAt"",""ModifiedAt"",""IsCustomer"",""IsSupplier"",""LeadStatus"",""LeadConvertedAt"",""MigrationSupplierId"")
SELECT s.""OrganizationId"", s.""Name"", NULL, s.""ContactPerson"", s.""Phone"", s.""Email"", s.""Address"", s.""Notes"", NULL, s.""IsActive"", NULL, s.""CreatedAt"", s.""ModifiedAt"", false, true, NULL, NULL, s.""Id""
FROM ""Suppliers"" s;

UPDATE ""InventoryMovements"" im SET ""SupplierContactId"" = c.""Id""
FROM ""Contacts"" c
WHERE c.""MigrationSupplierId"" IS NOT NULL AND c.""MigrationSupplierId"" = im.""SupplierId"";

ALTER TABLE ""Contacts"" DROP COLUMN IF EXISTS ""MigrationSupplierId"";

UPDATE ""Contacts"" c SET ""LeadConvertedAt"" = l.""ConvertedAt"", ""LeadStatus"" = NULL
FROM ""Leads"" l
WHERE l.""ConvertedToContactId"" IS NOT NULL AND l.""ConvertedToContactId"" = c.""Id"";

INSERT INTO ""Contacts"" (""OrganizationId"",""Name"",""Company"",""ContactPerson"",""Phone"",""Email"",""Address"",""Notes"",""Origin"",""IsActive"",""AssignedUserId"",""CreatedAt"",""ModifiedAt"",""IsCustomer"",""IsSupplier"",""LeadStatus"",""LeadConvertedAt"")
SELECT l.""OrganizationId"", l.""Name"", l.""Company"", l.""ContactPerson"", l.""Phone"", l.""Email"", NULL, l.""Notes"", l.""Origin"", true, l.""AssignedUserId"", l.""CreatedAt"", l.""ModifiedAt"", true, false, l.""Status"", NULL
FROM ""Leads"" l WHERE l.""ConvertedToContactId"" IS NULL;
");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Suppliers_SupplierId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_SupplierId",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.CreateTable(
                name: "CustomerLoyaltyAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    ContactId = table.Column<int>(type: "integer", nullable: false),
                    PointsBalance = table.Column<int>(type: "integer", nullable: false),
                    LifetimeOrders = table.Column<int>(type: "integer", nullable: false),
                    LastPurchaseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLoyaltyAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerLoyaltyAccounts_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerLoyaltyAccounts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    ContactId = table.Column<int>(type: "integer", nullable: false),
                    SaleOrderId = table.Column<int>(type: "integer", nullable: true),
                    PointsDelta = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyEvents_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyEvents_SaleOrders_SaleOrderId",
                        column: x => x.SaleOrderId,
                        principalTable: "SaleOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    PointsPerOrder = table.Column<int>(type: "integer", nullable: false),
                    NotifyEveryNOrders = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltySettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltySettings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLoyaltyAccounts_ContactId",
                table: "CustomerLoyaltyAccounts",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLoyaltyAccounts_OrganizationId_ContactId",
                table: "CustomerLoyaltyAccounts",
                columns: new[] { "OrganizationId", "ContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyEvents_ContactId",
                table: "LoyaltyEvents",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyEvents_OrganizationId",
                table: "LoyaltyEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyEvents_SaleOrderId",
                table: "LoyaltyEvents",
                column: "SaleOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltySettings_OrganizationId",
                table: "LoyaltySettings",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_SupplierContactId",
                table: "InventoryMovements",
                column: "SupplierContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Contacts_SupplierContactId",
                table: "InventoryMovements",
                column: "SupplierContactId",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Contacts_SupplierContactId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_SupplierContactId",
                table: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "CustomerLoyaltyAccounts");

            migrationBuilder.DropTable(
                name: "LoyaltyEvents");

            migrationBuilder.DropTable(
                name: "LoyaltySettings");

            migrationBuilder.DropColumn(
                name: "SupplierContactId",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "MobileListLayout",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsCustomer",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "IsSupplier",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "LeadConvertedAt",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "LeadStatus",
                table: "Contacts");

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "InventoryMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignedUserId = table.Column<int>(type: "integer", nullable: true),
                    ConvertedToContactId = table.Column<int>(type: "integer", nullable: true),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Company = table.Column<string>(type: "text", nullable: true),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Origin = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Contacts_ConvertedToContactId",
                        column: x => x.ConvertedToContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Leads_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_OrganizationId",
                table: "Suppliers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_AssignedUserId",
                table: "Leads",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ConvertedToContactId",
                table: "Leads",
                column: "ConvertedToContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_OrganizationId",
                table: "Leads",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_SupplierId",
                table: "InventoryMovements",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Suppliers_SupplierId",
                table: "InventoryMovements",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }
    }
}

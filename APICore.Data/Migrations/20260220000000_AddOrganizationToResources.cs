using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationToResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Product
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Products",
                type: "int",
                nullable: true);
            migrationBuilder.Sql(@"
                UPDATE Products SET OrganizationId = (SELECT TOP 1 Id FROM Organizations);
            ");
            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Products_OrganizationId",
                table: "Products",
                column: "OrganizationId");
            migrationBuilder.AddForeignKey(
                name: "FK_Products_Organizations_OrganizationId",
                table: "Products",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ProductCategory
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "ProductCategories",
                type: "int",
                nullable: true);
            migrationBuilder.Sql(@"
                UPDATE ProductCategories SET OrganizationId = (SELECT TOP 1 Id FROM Organizations);
            ");
            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "ProductCategories",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_OrganizationId",
                table: "ProductCategories",
                column: "OrganizationId");
            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Organizations_OrganizationId",
                table: "ProductCategories",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Supplier
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Suppliers",
                type: "int",
                nullable: true);
            migrationBuilder.Sql(@"
                UPDATE Suppliers SET OrganizationId = (SELECT TOP 1 Id FROM Organizations);
            ");
            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "Suppliers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_OrganizationId",
                table: "Suppliers",
                column: "OrganizationId");
            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Organizations_OrganizationId",
                table: "Suppliers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Role (nullable - system roles have null)
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Roles",
                type: "int",
                nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Roles_OrganizationId",
                table: "Roles",
                column: "OrganizationId");
            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Organizations_OrganizationId",
                table: "Roles",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Setting (nullable - global settings have null)
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Setting",
                type: "int",
                nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Setting_OrganizationId",
                table: "Setting",
                column: "OrganizationId");

            // Log (nullable)
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Log",
                type: "int",
                nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Log_OrganizationId",
                table: "Log",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Organizations_OrganizationId",
                table: "Products");
            migrationBuilder.DropIndex(
                name: "IX_Products_OrganizationId",
                table: "Products");
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Organizations_OrganizationId",
                table: "ProductCategories");
            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_OrganizationId",
                table: "ProductCategories");
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Organizations_OrganizationId",
                table: "Suppliers");
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_OrganizationId",
                table: "Suppliers");
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Organizations_OrganizationId",
                table: "Roles");
            migrationBuilder.DropIndex(
                name: "IX_Roles_OrganizationId",
                table: "Roles");
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Setting_OrganizationId",
                table: "Setting");
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Setting");

            migrationBuilder.DropIndex(
                name: "IX_Log_OrganizationId",
                table: "Log");
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Log");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Crear tabla Organizations primero
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            // 2) Insertar organización por defecto para datos existentes
            var now = DateTime.UtcNow;
            migrationBuilder.Sql($@"
                INSERT INTO Organizations (Name, Code, Description, CreatedAt, ModifiedAt)
                VALUES (N'Organización Principal', N'DEFAULT', N'Organización por defecto', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}');
            ");

            // 3) Añadir columna OrganizationId (nullable primero para poder actualizar filas existentes)
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Locations",
                type: "int",
                nullable: true);

            // 4) Asignar todas las localizaciones existentes a la organización por defecto
            migrationBuilder.Sql(@"
                UPDATE Locations SET OrganizationId = (SELECT TOP 1 Id FROM Organizations WHERE Code = N'DEFAULT')
                WHERE OrganizationId IS NULL;
            ");

            // 5) Hacer la columna NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "Locations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_OrganizationId",
                table: "Locations",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Organizations_OrganizationId",
                table: "Locations",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Organizations_OrganizationId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_OrganizationId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Locations");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodInstrumentReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstrumentReference",
                table: "PaymentMethods",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstrumentReference",
                table: "PaymentMethods");
        }
    }
}

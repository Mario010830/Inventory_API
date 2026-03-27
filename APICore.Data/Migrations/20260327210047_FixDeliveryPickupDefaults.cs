using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APICore.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixDeliveryPickupDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Locations\" SET \"OffersDelivery\" = true, \"OffersPickup\" = true;");

            migrationBuilder.AlterColumn<bool>(
                name: "OffersDelivery",
                table: "Locations",
                defaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "OffersPickup",
                table: "Locations",
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

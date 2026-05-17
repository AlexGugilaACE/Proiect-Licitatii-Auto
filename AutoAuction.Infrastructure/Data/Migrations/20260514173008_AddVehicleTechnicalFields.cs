using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoAuction.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTechnicalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EngineCapacityCm3",
                table: "Auctions",
                type: "int",
                nullable: false,
                defaultValue: 1600);

            migrationBuilder.AddColumn<int>(
                name: "HorsePower",
                table: "Auctions",
                type: "int",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.AddColumn<string>(
                name: "Vin",
                table: "Auctions",
                type: "nvarchar(17)",
                maxLength: 17,
                nullable: false,
                defaultValue: "UNKNOWNVIN0000000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EngineCapacityCm3",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "HorsePower",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "Vin",
                table: "Auctions");
        }
    }
}

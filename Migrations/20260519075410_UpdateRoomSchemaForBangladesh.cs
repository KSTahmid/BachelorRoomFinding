using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BachelorRoomFinding.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomSchemaForBangladesh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Rent",
                table: "Rooms",
                newName: "WiFiBill");

            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityBill",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GasBill",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MealCost",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyRent",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SeatRent",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceCharge",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterBill",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectricityBill",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GasBill",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MealCost",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MonthlyRent",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "SeatRent",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "ServiceCharge",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "WaterBill",
                table: "Rooms");

            migrationBuilder.RenameColumn(
                name: "WiFiBill",
                table: "Rooms",
                newName: "Rent");
        }
    }
}

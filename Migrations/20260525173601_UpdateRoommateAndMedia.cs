using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BachelorRoomFinding.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoommateAndMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVideo",
                table: "RoomPhotos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "RoommateConnectionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "RoommateConnectionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTransactionId",
                table: "RoommateConnectionRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdvancePaymentAmount",
                table: "RoommateAds",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "RoommateAds",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoommateAds_RoomId",
                table: "RoommateAds",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoommateAds_Rooms_RoomId",
                table: "RoommateAds",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoommateAds_Rooms_RoomId",
                table: "RoommateAds");

            migrationBuilder.DropIndex(
                name: "IX_RoommateAds_RoomId",
                table: "RoommateAds");

            migrationBuilder.DropColumn(
                name: "IsVideo",
                table: "RoomPhotos");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "RoommateConnectionRequests");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "RoommateConnectionRequests");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "RoommateConnectionRequests");

            migrationBuilder.DropColumn(
                name: "AdvancePaymentAmount",
                table: "RoommateAds");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "RoommateAds");
        }
    }
}

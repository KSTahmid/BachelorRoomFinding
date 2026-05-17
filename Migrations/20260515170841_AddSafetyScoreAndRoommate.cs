using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BachelorRoomFinding.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyScoreAndRoommate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SafetyScore",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerifiedTenantReview",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReviewTags",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterUserId = table.Column<int>(type: "int", nullable: false),
                    TargetRoomId = table.Column<int>(type: "int", nullable: true),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Rooms_TargetRoomId",
                        column: x => x.TargetRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoommateAds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PreferredAreas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxRentPerPerson = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoveInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfRoommatesNeeded = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoommateAds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoommateAds_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Smoking = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SleepSchedule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cleanliness = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FoodHabit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrayerHabit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuestPolicy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PetFriendly = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoommateConnectionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    RoommateAdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoommateConnectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoommateConnectionRequests_RoommateAds_RoommateAdId",
                        column: x => x.RoommateAdId,
                        principalTable: "RoommateAds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoommateConnectionRequests_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterUserId",
                table: "Reports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_TargetRoomId",
                table: "Reports",
                column: "TargetRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_TargetUserId",
                table: "Reports",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoommateAds_UserId",
                table: "RoommateAds",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoommateConnectionRequests_RoommateAdId",
                table: "RoommateConnectionRequests",
                column: "RoommateAdId");

            migrationBuilder.CreateIndex(
                name: "IX_RoommateConnectionRequests_SenderUserId",
                table: "RoommateConnectionRequests",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "RoommateConnectionRequests");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "RoommateAds");

            migrationBuilder.DropColumn(
                name: "SafetyScore",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IsVerifiedTenantReview",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReviewTags",
                table: "Reviews");
        }
    }
}

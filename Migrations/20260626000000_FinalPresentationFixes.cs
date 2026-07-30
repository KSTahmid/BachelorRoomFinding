using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BachelorRoomFinding.Migrations
{
    public partial class FinalPresentationFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BkashNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoNumber",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NagadNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "RoommateConnectionRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientWalletNumber",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderWalletNumber",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptImagePath",
                table: "MessExpenses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "MessGroups",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql("UPDATE MessGroups SET InviteCode = CONCAT('MB', RIGHT('000000' + CAST(Id AS varchar(6)), 6)) WHERE InviteCode IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "InviteCode",
                table: "MessGroups",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "MessMembers",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "MessFundEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessGroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessFundEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessFundEntries_MessGroups_MessGroupId",
                        column: x => x.MessGroupId,
                        principalTable: "MessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessFundEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessNotices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessGroupId = table.Column<int>(type: "int", nullable: false),
                    PostedByUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessNotices_MessGroups_MessGroupId",
                        column: x => x.MessGroupId,
                        principalTable: "MessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessNotices_Users_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessRosterItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessGroupId = table.Column<int>(type: "int", nullable: false),
                    AssignedUserId = table.Column<int>(type: "int", nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MenuOrNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessRosterItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessRosterItems_MessGroups_MessGroupId",
                        column: x => x.MessGroupId,
                        principalTable: "MessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessRosterItems_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OwnerId",
                table: "Payments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RoomId",
                table: "Payments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MessGroups_InviteCode",
                table: "MessGroups",
                column: "InviteCode",
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_MessGroups_RoomId",
                table: "MessGroups");

            migrationBuilder.CreateIndex(
                name: "IX_MessGroups_RoomId",
                table: "MessGroups",
                column: "RoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessMembers_MessGroupId_UserId",
                table: "MessMembers",
                columns: new[] { "MessGroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessFundEntries_MessGroupId",
                table: "MessFundEntries",
                column: "MessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MessFundEntries_UserId",
                table: "MessFundEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessNotices_MessGroupId",
                table: "MessNotices",
                column: "MessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MessNotices_PostedByUserId",
                table: "MessNotices",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessRosterItems_AssignedUserId",
                table: "MessRosterItems",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessRosterItems_MessGroupId",
                table: "MessRosterItems",
                column: "MessGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Rooms_RoomId",
                table: "Payments",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_OwnerId",
                table: "Payments",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Payments_Rooms_RoomId", table: "Payments");
            migrationBuilder.DropForeignKey(name: "FK_Payments_Users_OwnerId", table: "Payments");

            migrationBuilder.DropTable(name: "MessFundEntries");
            migrationBuilder.DropTable(name: "MessNotices");
            migrationBuilder.DropTable(name: "MessRosterItems");

            migrationBuilder.DropIndex(name: "IX_Payments_OwnerId", table: "Payments");
            migrationBuilder.DropIndex(name: "IX_Payments_RoomId", table: "Payments");
            migrationBuilder.DropIndex(name: "IX_MessGroups_InviteCode", table: "MessGroups");
            migrationBuilder.DropIndex(name: "IX_MessGroups_RoomId", table: "MessGroups");
            migrationBuilder.DropIndex(name: "IX_MessMembers_MessGroupId_UserId", table: "MessMembers");

            migrationBuilder.CreateIndex(
                name: "IX_MessGroups_RoomId",
                table: "MessGroups",
                column: "RoomId");

            migrationBuilder.DropColumn(name: "BkashNumber", table: "Users");
            migrationBuilder.DropColumn(name: "IsDemoNumber", table: "Users");
            migrationBuilder.DropColumn(name: "NagadNumber", table: "Users");
            migrationBuilder.DropColumn(name: "RespondedAt", table: "RoommateConnectionRequests");
            migrationBuilder.DropColumn(name: "OwnerId", table: "Payments");
            migrationBuilder.DropColumn(name: "RecipientWalletNumber", table: "Payments");
            migrationBuilder.DropColumn(name: "RoomId", table: "Payments");
            migrationBuilder.DropColumn(name: "SenderWalletNumber", table: "Payments");
            migrationBuilder.DropColumn(name: "ReceiptImagePath", table: "MessExpenses");
            migrationBuilder.DropColumn(name: "InviteCode", table: "MessGroups");
            migrationBuilder.DropColumn(name: "Role", table: "MessMembers");
        }
    }
}

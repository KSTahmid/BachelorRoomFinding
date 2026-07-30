using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BachelorRoomFinding.Migrations
{
    /// <inheritdoc />
    public partial class AddMessEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    ManagerUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessGroups_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessGroups_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessGroupId = table.Column<int>(type: "int", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessExpenses_MessGroups_MessGroupId",
                        column: x => x.MessGroupId,
                        principalTable: "MessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessExpenses_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessGroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsManager = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessMembers_MessGroups_MessGroupId",
                        column: x => x.MessGroupId,
                        principalTable: "MessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessExpenseShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessExpenseId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessExpenseShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessExpenseShares_MessExpenses_MessExpenseId",
                        column: x => x.MessExpenseId,
                        principalTable: "MessExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessExpenseShares_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessExpenses_AddedByUserId",
                table: "MessExpenses",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessExpenses_MessGroupId",
                table: "MessExpenses",
                column: "MessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MessExpenseShares_MessExpenseId",
                table: "MessExpenseShares",
                column: "MessExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_MessExpenseShares_UserId",
                table: "MessExpenseShares",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessGroups_ManagerUserId",
                table: "MessGroups",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessGroups_RoomId",
                table: "MessGroups",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MessMembers_MessGroupId",
                table: "MessMembers",
                column: "MessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MessMembers_UserId",
                table: "MessMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessExpenseShares");

            migrationBuilder.DropTable(
                name: "MessMembers");

            migrationBuilder.DropTable(
                name: "MessExpenses");

            migrationBuilder.DropTable(
                name: "MessGroups");
        }
    }
}

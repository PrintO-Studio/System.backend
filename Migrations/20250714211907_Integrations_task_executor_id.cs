using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Integrations_task_executor_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "success",
                table: "ozonIntegrationTasks",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AddColumn<int>(
                name: "exectionUserId",
                table: "ozonIntegrationTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "executionDate",
                table: "ozonIntegrationTasks",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_ozonIntegrationTasks_exectionUserId",
                table: "ozonIntegrationTasks",
                column: "exectionUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ozonIntegrationTasks_Users_exectionUserId",
                table: "ozonIntegrationTasks",
                column: "exectionUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ozonIntegrationTasks_Users_exectionUserId",
                table: "ozonIntegrationTasks");

            migrationBuilder.DropIndex(
                name: "IX_ozonIntegrationTasks_exectionUserId",
                table: "ozonIntegrationTasks");

            migrationBuilder.DropColumn(
                name: "exectionUserId",
                table: "ozonIntegrationTasks");

            migrationBuilder.DropColumn(
                name: "executionDate",
                table: "ozonIntegrationTasks");

            migrationBuilder.AlterColumn<bool>(
                name: "success",
                table: "ozonIntegrationTasks",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true);
        }
    }
}

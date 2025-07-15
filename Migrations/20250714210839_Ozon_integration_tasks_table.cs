using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Ozon_integration_tasks_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OzonIntegrationTask_figurines_figurineId",
                table: "OzonIntegrationTask");

            migrationBuilder.DropForeignKey(
                name: "FK_products_OzonIntegrationTask_ozonLastTaskId",
                table: "products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OzonIntegrationTask",
                table: "OzonIntegrationTask");

            migrationBuilder.RenameTable(
                name: "OzonIntegrationTask",
                newName: "ozonIntegrationTasks");

            migrationBuilder.RenameIndex(
                name: "IX_OzonIntegrationTask_figurineId",
                table: "ozonIntegrationTasks",
                newName: "IX_ozonIntegrationTasks_figurineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ozonIntegrationTasks",
                table: "ozonIntegrationTasks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ozonIntegrationTasks_figurines_figurineId",
                table: "ozonIntegrationTasks",
                column: "figurineId",
                principalTable: "figurines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_products_ozonIntegrationTasks_ozonLastTaskId",
                table: "products",
                column: "ozonLastTaskId",
                principalTable: "ozonIntegrationTasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ozonIntegrationTasks_figurines_figurineId",
                table: "ozonIntegrationTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_products_ozonIntegrationTasks_ozonLastTaskId",
                table: "products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ozonIntegrationTasks",
                table: "ozonIntegrationTasks");

            migrationBuilder.RenameTable(
                name: "ozonIntegrationTasks",
                newName: "OzonIntegrationTask");

            migrationBuilder.RenameIndex(
                name: "IX_ozonIntegrationTasks_figurineId",
                table: "OzonIntegrationTask",
                newName: "IX_OzonIntegrationTask_figurineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OzonIntegrationTask",
                table: "OzonIntegrationTask",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OzonIntegrationTask_figurines_figurineId",
                table: "OzonIntegrationTask",
                column: "figurineId",
                principalTable: "figurines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_products_OzonIntegrationTask_ozonLastTaskId",
                table: "products",
                column: "ozonLastTaskId",
                principalTable: "OzonIntegrationTask",
                principalColumn: "Id");
        }
    }
}

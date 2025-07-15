using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Integration_task_figurine_to_product : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ozonIntegrationTasks_figurines_figurineId",
                table: "ozonIntegrationTasks");

            migrationBuilder.RenameColumn(
                name: "figurineId",
                table: "ozonIntegrationTasks",
                newName: "productId");

            migrationBuilder.RenameIndex(
                name: "IX_ozonIntegrationTasks_figurineId",
                table: "ozonIntegrationTasks",
                newName: "IX_ozonIntegrationTasks_productId");

            migrationBuilder.AddForeignKey(
                name: "FK_ozonIntegrationTasks_products_productId",
                table: "ozonIntegrationTasks",
                column: "productId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ozonIntegrationTasks_products_productId",
                table: "ozonIntegrationTasks");

            migrationBuilder.RenameColumn(
                name: "productId",
                table: "ozonIntegrationTasks",
                newName: "figurineId");

            migrationBuilder.RenameIndex(
                name: "IX_ozonIntegrationTasks_productId",
                table: "ozonIntegrationTasks",
                newName: "IX_ozonIntegrationTasks_figurineId");

            migrationBuilder.AddForeignKey(
                name: "FK_ozonIntegrationTasks_figurines_figurineId",
                table: "ozonIntegrationTasks",
                column: "figurineId",
                principalTable: "figurines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

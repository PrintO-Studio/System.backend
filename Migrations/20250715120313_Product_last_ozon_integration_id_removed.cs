using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Product_last_ozon_integration_id_removed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_ozonIntegrationTasks_ozonLastTaskId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_ozonLastTaskId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ozonLastTaskId",
                table: "products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ozonLastTaskId",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_ozonLastTaskId",
                table: "products",
                column: "ozonLastTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_products_ozonIntegrationTasks_ozonLastTaskId",
                table: "products",
                column: "ozonLastTaskId",
                principalTable: "ozonIntegrationTasks",
                principalColumn: "Id");
        }
    }
}

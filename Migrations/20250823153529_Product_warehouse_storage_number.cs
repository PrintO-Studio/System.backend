using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Product_warehouse_storage_number : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "warehouseStorageNumber",
                table: "products",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "warehouseStorageNumber",
                table: "products");
        }
    }
}

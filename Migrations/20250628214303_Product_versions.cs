using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintO.Migrations
{
    /// <inheritdoc />
    public partial class Product_versions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "ozonIntegrationVersion",
                table: "products",
                type: "int unsigned",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "productVersion",
                table: "products",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "wildberriesIntegrationVersion",
                table: "products",
                type: "int unsigned",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "yandexIntegrationVersion",
                table: "products",
                type: "int unsigned",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ozonIntegrationVersion",
                table: "products");

            migrationBuilder.DropColumn(
                name: "productVersion",
                table: "products");

            migrationBuilder.DropColumn(
                name: "wildberriesIntegrationVersion",
                table: "products");

            migrationBuilder.DropColumn(
                name: "yandexIntegrationVersion",
                table: "products");
        }
    }
}

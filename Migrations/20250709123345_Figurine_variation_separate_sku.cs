using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Figurine_variation_separate_sku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "separateSKU",
                table: "figurineVariations",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "separateSKU",
                table: "figurineVariations");
        }
    }
}

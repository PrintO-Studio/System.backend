using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Figurine_variation_height : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<uint>(
                name: "heightMm",
                table: "figurineVariations",
                type: "int unsigned",
                nullable: true,
                oldClrType: typeof(uint),
                oldType: "int unsigned");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<uint>(
                name: "heightMm",
                table: "figurineVariations",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u,
                oldClrType: typeof(uint),
                oldType: "int unsigned",
                oldNullable: true);
        }
    }
}

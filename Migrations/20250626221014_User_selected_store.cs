using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintO.Migrations
{
    /// <inheritdoc />
    public partial class User_selected_store : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "selectedStoreId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_selectedStoreId",
                table: "Users",
                column: "selectedStoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_stores_selectedStoreId",
                table: "Users",
                column: "selectedStoreId",
                principalTable: "stores",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_stores_selectedStoreId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_selectedStoreId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "selectedStoreId",
                table: "Users");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintO.Migrations
{
    /// <inheritdoc />
    public partial class Entity_id_field : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreUser_stores_membershipsid",
                table: "StoreUser");

            migrationBuilder.RenameColumn(
                name: "membershipsid",
                table: "StoreUser",
                newName: "membershipsId");

            migrationBuilder.RenameIndex(
                name: "IX_StoreUser_membershipsid",
                table: "StoreUser",
                newName: "IX_StoreUser_membershipsId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "stores",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "products",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "invitationTokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "images",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "files",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "figurineVariations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "figurines",
                newName: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUser_stores_membershipsId",
                table: "StoreUser",
                column: "membershipsId",
                principalTable: "stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreUser_stores_membershipsId",
                table: "StoreUser");

            migrationBuilder.RenameColumn(
                name: "membershipsId",
                table: "StoreUser",
                newName: "membershipsid");

            migrationBuilder.RenameIndex(
                name: "IX_StoreUser_membershipsId",
                table: "StoreUser",
                newName: "IX_StoreUser_membershipsid");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "stores",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "products",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "invitationTokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "images",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "files",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "figurineVariations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "figurines",
                newName: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUser_stores_membershipsid",
                table: "StoreUser",
                column: "membershipsid",
                principalTable: "stores",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

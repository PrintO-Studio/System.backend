using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintO.Migrations
{
    /// <inheritdoc />
    public partial class Invitation_token_creator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invitationTokens_Users_createdByUserId",
                table: "invitationTokens");

            migrationBuilder.DropIndex(
                name: "IX_invitationTokens_createdByUserId",
                table: "invitationTokens");

            migrationBuilder.DropColumn(
                name: "createdByUserId",
                table: "invitationTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "createdByUserId",
                table: "invitationTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_invitationTokens_createdByUserId",
                table: "invitationTokens",
                column: "createdByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_invitationTokens_Users_createdByUserId",
                table: "invitationTokens",
                column: "createdByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

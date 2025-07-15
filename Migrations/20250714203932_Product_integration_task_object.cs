using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Product_integration_task_object : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ozonIntegrationVersion",
                table: "products");

            migrationBuilder.DropColumn(
                name: "wildberriesIntegrationVersion",
                table: "products");

            migrationBuilder.DropColumn(
                name: "yandexIntegrationVersion",
                table: "products");

            migrationBuilder.AddColumn<int>(
                name: "ozonLastTaskId",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OzonIntegrationTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    taskId = table.Column<long>(type: "bigint", nullable: false),
                    inProgress = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    logs = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version = table.Column<uint>(type: "int unsigned", nullable: true),
                    figurineId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OzonIntegrationTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OzonIntegrationTask_figurines_figurineId",
                        column: x => x.figurineId,
                        principalTable: "figurines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_products_ozonLastTaskId",
                table: "products",
                column: "ozonLastTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OzonIntegrationTask_figurineId",
                table: "OzonIntegrationTask",
                column: "figurineId");

            migrationBuilder.AddForeignKey(
                name: "FK_products_OzonIntegrationTask_ozonLastTaskId",
                table: "products",
                column: "ozonLastTaskId",
                principalTable: "OzonIntegrationTask",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_OzonIntegrationTask_ozonLastTaskId",
                table: "products");

            migrationBuilder.DropTable(
                name: "OzonIntegrationTask");

            migrationBuilder.DropIndex(
                name: "IX_products_ozonLastTaskId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ozonLastTaskId",
                table: "products");

            migrationBuilder.AddColumn<uint>(
                name: "ozonIntegrationVersion",
                table: "products",
                type: "int unsigned",
                nullable: true);

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
    }
}

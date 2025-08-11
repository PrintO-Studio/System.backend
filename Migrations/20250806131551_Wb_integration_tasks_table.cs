using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintOSystem.Migrations
{
    /// <inheritdoc />
    public partial class Wb_integration_tasks_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wbIntegrationTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    productId = table.Column<int>(type: "int", nullable: false),
                    inProgress = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    success = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    logs = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version = table.Column<uint>(type: "int unsigned", nullable: true),
                    exectionUserId = table.Column<int>(type: "int", nullable: false),
                    executionDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wbIntegrationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wbIntegrationTasks_Users_exectionUserId",
                        column: x => x.exectionUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_wbIntegrationTasks_products_productId",
                        column: x => x.productId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_wbIntegrationTasks_exectionUserId",
                table: "wbIntegrationTasks",
                column: "exectionUserId");

            migrationBuilder.CreateIndex(
                name: "IX_wbIntegrationTasks_productId",
                table: "wbIntegrationTasks",
                column: "productId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wbIntegrationTasks");
        }
    }
}

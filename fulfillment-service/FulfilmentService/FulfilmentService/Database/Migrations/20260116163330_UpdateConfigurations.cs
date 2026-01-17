using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FulfilmentService.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FulfilmentTasks_Workers_WorkerId",
                table: "FulfilmentTasks");

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "FulfilmentTasks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FulfilmentTasks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_FulfilmentTasks_OrderId",
                table: "FulfilmentTasks",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FulfilmentTasks_Workers_WorkerId",
                table: "FulfilmentTasks",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FulfilmentTasks_Workers_WorkerId",
                table: "FulfilmentTasks");

            migrationBuilder.DropIndex(
                name: "IX_FulfilmentTasks_OrderId",
                table: "FulfilmentTasks");

            migrationBuilder.AlterColumn<int>(
                name: "WorkerId",
                table: "FulfilmentTasks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "FulfilmentTasks",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_FulfilmentTasks_Workers_WorkerId",
                table: "FulfilmentTasks",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

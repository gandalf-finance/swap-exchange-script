using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Scripts.Dividends.Migrations
{
    public partial class modify_swapTransactionRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "swap_result");

            migrationBuilder.AddColumn<string>(
                name: "MethodName",
                table: "swap_result",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ToAddress",
                table: "swap_result",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MethodName",
                table: "swap_result");

            migrationBuilder.DropColumn(
                name: "ToAddress",
                table: "swap_result");

            migrationBuilder.AddColumn<int>(
                name: "TransactionType",
                table: "swap_result",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Scripts.Dividends.Migrations
{
    public partial class tx_check : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TransactionStatus",
                table: "swap_result",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TransactionType",
                table: "swap_result",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionStatus",
                table: "swap_result");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "swap_result");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPriceAndUserPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanAccessMobilePOS",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanChangeThemeAndLanguage",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanConfigureFiscalReceipt",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanOpenCashDrawer",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanAccessMobilePOS",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanChangeThemeAndLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanConfigureFiscalReceipt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanOpenCashDrawer",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AlignPosSalesAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginChannel",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "SaleDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "SaleDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountReceived",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CashSessionId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeAmount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyId_CashSessionId",
                table: "Payments",
                columns: new[] { "CompanyId", "CashSessionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_CompanyId_CashSessionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OriginChannel",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "AmountReceived",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CashSessionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ChangeAmount",
                table: "Payments");
        }
    }
}

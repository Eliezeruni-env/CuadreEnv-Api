using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_SaleId",
                table: "SaleDetails");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CompanyId_Date",
                table: "Sales",
                columns: new[] { "CompanyId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_SaleId_ProductId",
                table: "SaleDetails",
                columns: new[] { "SaleId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Description",
                table: "Products",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_CompanyId_Date",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_SaleId_ProductId",
                table: "SaleDetails");

            migrationBuilder.DropIndex(
                name: "IX_Products_Description",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_SaleId",
                table: "SaleDetails",
                column: "SaleId");
        }
    }
}

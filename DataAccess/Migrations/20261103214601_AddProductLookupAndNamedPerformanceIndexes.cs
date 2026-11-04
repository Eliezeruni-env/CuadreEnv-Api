using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductLookupAndNamedPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_CompanyId_Date",
                table: "Sales");

            migrationBuilder.RenameIndex(
                name: "IX_SaleDetails_SaleId_ProductId",
                table: "SaleDetails",
                newName: "idx_saledetails_sale_product");

            migrationBuilder.CreateIndex(
                name: "idx_sales_company_date",
                table: "Sales",
                columns: new[] { "CompanyId", "Date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_products_company_code",
                table: "Products",
                columns: new[] { "CompanyId", "IsDeleted", "Active", "Barcode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_sales_company_date",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "idx_products_company_code",
                table: "Products");

            migrationBuilder.RenameIndex(
                name: "idx_saledetails_sale_product",
                table: "SaleDetails",
                newName: "IX_SaleDetails_SaleId_ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CompanyId_Date",
                table: "Sales",
                columns: new[] { "CompanyId", "Date" });
        }
    }
}

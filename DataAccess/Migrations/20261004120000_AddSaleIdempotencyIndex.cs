using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations;

public partial class AddSaleIdempotencyIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Sales_CompanyId_IdempotencyKey_Active",
            table: "Sales",
            columns: new[] { "CompanyId", "IdempotencyKey", "IsDeleted" },
            unique: true,
            filter: "[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Sales_CompanyId_IdempotencyKey_Active",
            table: "Sales");
    }
}

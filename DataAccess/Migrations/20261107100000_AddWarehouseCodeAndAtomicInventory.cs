using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    public partial class AddWarehouseCodeAndAtomicInventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Warehouses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ""Warehouses""
                SET ""Code"" = 'WH-' || ""Id""
                WHERE ""Code"" = '' OR ""Code"" IS NULL;");

            migrationBuilder.Sql(@"
                WITH duplicates AS (
                    SELECT ""Id"", ""CompanyId"", ""ProductId"", ""WarehouseId"",
                           SUM(""Quantity"") OVER (PARTITION BY ""CompanyId"", ""ProductId"", ""WarehouseId"") AS total_quantity,
                           ROW_NUMBER() OVER (PARTITION BY ""CompanyId"", ""ProductId"", ""WarehouseId"" ORDER BY ""Id"") AS row_number
                    FROM ""Inventories""
                ), survivors AS (
                    SELECT ""Id"", total_quantity
                    FROM duplicates
                    WHERE row_number = 1
                )
                UPDATE ""Inventories"" inventory
                SET ""Quantity"" = survivors.total_quantity
                FROM survivors
                WHERE inventory.""Id"" = survivors.""Id"";

                DELETE FROM ""Inventories"" inventory
                USING (
                    SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""CompanyId"", ""ProductId"", ""WarehouseId"" ORDER BY ""Id"") AS row_number
                    FROM ""Inventories""
                ) duplicates
                WHERE inventory.""Id"" = duplicates.""Id"" AND duplicates.row_number > 1;");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CompanyId_Code",
                table: "Warehouses",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_CompanyId_ProductId_WarehouseId",
                table: "Inventories",
                columns: new[] { "CompanyId", "ProductId", "WarehouseId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventories_CompanyId_ProductId_WarehouseId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_CompanyId_Code",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Warehouses");
        }
    }
}

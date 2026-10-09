using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations;

public partial class AddSaleIdempotencyIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sales_CompanyId_IdempotencyKey_IsDeleted' AND object_id = OBJECT_ID(N'[Sales]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Sales_CompanyId_IdempotencyKey_IsDeleted]
        ON [Sales] ([CompanyId], [IdempotencyKey], [IsDeleted])
        WHERE [IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0;
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sales_CompanyId_IdempotencyKey_IsDeleted' AND object_id = OBJECT_ID(N'[Sales]'))
BEGIN
    DROP INDEX [IX_Sales_CompanyId_IdempotencyKey_IsDeleted] ON [Sales];
END
");
    }
}

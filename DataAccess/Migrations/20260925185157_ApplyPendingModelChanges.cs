using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ApplyPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // rename index with conditional check to avoid ambiguous object errors on SQL Server
            migrationBuilder.Sql(@"
IF EXISTS(
    SELECT 1 FROM sys.indexes i
    JOIN sys.objects o ON i.object_id = o.object_id
    WHERE i.name = 'IX_Sales_CompanyId_IdempotencyKey_Active' AND o.name = 'Sales')
BEGIN
    EXEC sp_rename N'[Sales].[IX_Sales_CompanyId_IdempotencyKey_Active]', N'IX_Sales_CompanyId_IdempotencyKey_IsDeleted', 'INDEX';
END
");

            // Skipping RowVersion alteration because changing to rowversion/timestamp can fail on some SQL Server setups.
            // No action required here when the database already uses an appropriate concurrency token.

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[Sales]')
      AND name = N'IdempotencyKey'
      AND max_length = -1)
BEGIN
    ALTER TABLE [Sales] ALTER COLUMN [IdempotencyKey] nvarchar(450) NULL;
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sales_CompanyId_IdempotencyKey_IsDeleted' AND object_id = OBJECT_ID(N'[Sales]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Sales_CompanyId_IdempotencyKey_IsDeleted]
        ON [Sales] ([CompanyId], [IdempotencyKey], [IsDeleted])
        WHERE [IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sales_CompanyId_IdempotencyKey_IsDeleted' AND object_id = OBJECT_ID(N'[Sales]'))
BEGIN
    DROP INDEX [IX_Sales_CompanyId_IdempotencyKey_IsDeleted] ON [Sales];
END
");

            migrationBuilder.Sql(@"
IF EXISTS(
    SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[Sales]') AND c.name = 'RowVersion' AND t.name = 'rowversion')
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sales]') AND [c].[name] = N'RowVersion');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Sales] DROP CONSTRAINT [' + @var0 + ']');
    ALTER TABLE [Sales] ALTER COLUMN [RowVersion] varbinary(max) NULL;
END
");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[Sales]')
      AND name = N'IdempotencyKey'
      AND max_length <> -1)
BEGIN
    ALTER TABLE [Sales] ALTER COLUMN [IdempotencyKey] nvarchar(max) NULL;
END
");
        }
    }
}

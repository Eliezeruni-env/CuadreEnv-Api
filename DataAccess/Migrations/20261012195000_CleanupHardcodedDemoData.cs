using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations;

/// <summary>
/// Hides the demo tenants and their tenant-scoped rows created by historical seed migrations.
/// No demo data is recreated on rollback.
/// </summary>
public partial class CleanupHardcodedDemoData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            CREATE TABLE #DemoCompanies (Id int PRIMARY KEY);
            INSERT INTO #DemoCompanies (Id)
            SELECT Id
            FROM Companies
            WHERE Rnc IN (N'DEMO-000000', N'RBAC-DEMO-A', N'RBAC-DEMO-B', N'LOCAL-AUTH-A', N'LOCAL-AUTH-B')
               OR Email IN (N'demo@cuadreenv.local', N'demo.a@cuadreenv.local', N'demo.b@cuadreenv.local',
                            N'local.auth.a@cuadreenv.local', N'local.auth.b@cuadreenv.local',
                            N'rbac.empresa.a@cuadreenv.local', N'rbac.empresa.b@cuadreenv.local');

            DECLARE @sql nvarchar(max) = N'';
            SELECT @sql = @sql +
                N'UPDATE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) +
                N' SET IsDeleted = 1, Active = 0 WHERE CompanyId IN (SELECT Id FROM #DemoCompanies);' + CHAR(13)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            INNER JOIN sys.columns companyColumn ON companyColumn.object_id = t.object_id AND companyColumn.name = N'CompanyId'
            INNER JOIN sys.columns deletedColumn ON deletedColumn.object_id = t.object_id AND deletedColumn.name = N'IsDeleted'
            INNER JOIN sys.columns activeColumn ON activeColumn.object_id = t.object_id AND activeColumn.name = N'Active'
             WHERE t.is_ms_shipped = 0
               AND t.name NOT IN (N'CashMovements', N'CashRegisters');
            IF @sql <> N'' EXEC sys.sp_executesql @sql;

            UPDATE Users
            SET IsDeleted = 1, Active = 0
            WHERE Email LIKE N'demo.%@cuadreenv.local'
               OR Email LIKE N'rbac.%@cuadreenv.local'
               OR Email LIKE N'hier.%@cuadreenv.local'
               OR Email LIKE N'local.%@cuadreenv.local';

            UPDATE Companies
            SET IsDeleted = 1, Active = 0
            WHERE Id IN (SELECT Id FROM #DemoCompanies)
               OR Name IN (N'Seed Company A', N'Seed Company B');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally do not restore demo data.
    }
}

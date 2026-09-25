using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedRbacRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @RoleId int;
DECLARE role_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT Id FROM Roles WHERE CompanyId IN (SELECT Id FROM Companies WHERE Rnc IN (N'RBAC-DEMO-A',N'RBAC-DEMO-B'));
OPEN role_cursor;
FETCH NEXT FROM role_cursor INTO @RoleId;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM Roles WHERE Id=@RoleId AND Name=N'RBAC Admin')
        INSERT INTO RolePermissions(RoleId,PermissionId)
        SELECT @RoleId,p.Id FROM Permissions p WHERE NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleId AND x.PermissionId=p.Id);
    ELSE IF EXISTS (SELECT 1 FROM Roles WHERE Id=@RoleId AND Name=N'RBAC Audit')
        INSERT INTO RolePermissions(RoleId,PermissionId)
        SELECT @RoleId,p.Id FROM Permissions p WHERE p.Module IN (N'Sales',N'Inventory',N'Customers',N'Audit') AND p.Action IN (N'View',N'Edit',N'Approve') AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleId AND x.PermissionId=p.Id);
    ELSE IF EXISTS (SELECT 1 FROM Roles WHERE Id=@RoleId AND Name=N'RBAC Sales Operator')
        INSERT INTO RolePermissions(RoleId,PermissionId)
        SELECT @RoleId,p.Id FROM Permissions p WHERE p.Module=N'Sales' AND p.Action IN (N'View',N'Create') AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleId AND x.PermissionId=p.Id);
    ELSE IF EXISTS (SELECT 1 FROM Roles WHERE Id=@RoleId AND Name=N'RBAC Inventory Manager')
        INSERT INTO RolePermissions(RoleId,PermissionId)
        SELECT @RoleId,p.Id FROM Permissions p WHERE p.Module=N'Inventory' AND p.Action IN (N'View',N'Edit') AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleId AND x.PermissionId=p.Id);
    FETCH NEXT FROM role_cursor INTO @RoleId;
END;
CLOSE role_cursor;
DEALLOCATE role_cursor;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

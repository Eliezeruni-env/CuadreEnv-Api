using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixHierarchicalDemoUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @CompanyA int, @CompanyB int;
SELECT @CompanyA=Id FROM Companies WHERE Rnc=N'RBAC-DEMO-A';
SELECT @CompanyB=Id FROM Companies WHERE Rnc=N'RBAC-DEMO-B';

IF @CompanyA IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Inventory Manager')
    INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy)
    VALUES (@CompanyA,N'RBAC Inventory Manager',N'Rol personalizado de inventario',0,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF @CompanyB IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Inventory Manager')
    INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy)
    VALUES (@CompanyB,N'RBAC Inventory Manager',N'Rol personalizado de inventario',0,@Now,NULL,@Now,1,0,N'seed',N'seed');

DECLARE @UserId int, @CompanyId int, @RoleId int;
SELECT @RoleId=Id FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Inventory Manager';
SELECT @UserId=Id FROM Users WHERE Email=N'hier.inventory.a@cuadreenv.local';
IF @UserId IS NOT NULL AND @RoleId IS NOT NULL
BEGIN
    DELETE FROM UserRoles WHERE UserId=@UserId AND RoleId IN (SELECT Id FROM Roles WHERE CompanyId=@CompanyA AND Name<>N'RBAC Inventory Manager');
    IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId=@UserId AND RoleId=@RoleId)
        INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) VALUES (@UserId,@RoleId,@CompanyA,@Now,NULL);
END;

SELECT @RoleId=Id FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Inventory Manager';
SELECT @UserId=Id FROM Users WHERE Email=N'hier.inventory.b@cuadreenv.local';
IF @UserId IS NOT NULL AND @RoleId IS NOT NULL
BEGIN
    DELETE FROM UserRoles WHERE UserId=@UserId AND RoleId IN (SELECT Id FROM Roles WHERE CompanyId=@CompanyB AND Name<>N'RBAC Inventory Manager');
    IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId=@UserId AND RoleId=@RoleId)
        INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) VALUES (@UserId,@RoleId,@CompanyB,@Now,NULL);
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM UserRoles WHERE UserId IN (SELECT Id FROM Users WHERE Email IN (N'hier.inventory.a@cuadreenv.local',N'hier.inventory.b@cuadreenv.local'));
DELETE FROM Roles WHERE Name=N'RBAC Inventory Manager' AND CompanyId IN (SELECT Id FROM Companies WHERE Rnc IN (N'RBAC-DEMO-A',N'RBAC-DEMO-B'));
");
        }
    }
}

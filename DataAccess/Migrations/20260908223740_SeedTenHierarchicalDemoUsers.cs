using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedTenHierarchicalDemoUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @Hash nvarchar(500) = N'$2a$11$0vkPUpySVVVe36tEDS1lneBomhzRhTmbzzjT.shJokZJ/ajRXL.uq';
DECLARE @CompanyA int, @CompanyB int;

SELECT @CompanyA = Id FROM Companies WHERE Rnc = N'RBAC-DEMO-A';
SELECT @CompanyB = Id FROM Companies WHERE Rnc = N'RBAC-DEMO-B';

IF @CompanyA IS NULL
BEGIN
    INSERT INTO Companies (Name,Rnc,Email,Address,Phone,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
    VALUES (N'CuadreEnv Demo A',N'RBAC-DEMO-A',N'demo.a@cuadreenv.local',N'Santo Domingo',N'000-100-0001',@Now,1,0,NULL,N'seed',N'seed');
    SET @CompanyA = CONVERT(int, SCOPE_IDENTITY());
END;

IF @CompanyB IS NULL
BEGIN
    INSERT INTO Companies (Name,Rnc,Email,Address,Phone,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
    VALUES (N'CuadreEnv Demo B',N'RBAC-DEMO-B',N'demo.b@cuadreenv.local',N'Santiago',N'000-100-0002',@Now,1,0,NULL,N'seed',N'seed');
    SET @CompanyB = CONVERT(int, SCOPE_IDENTITY());
END;

IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Employee')
    INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy)
    VALUES (@CompanyA,N'RBAC Employee',N'Operador básico del tenant',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Employee')
    INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy)
    VALUES (@CompanyB,N'RBAC Employee',N'Operador básico del tenant',1,@Now,NULL,@Now,1,0,N'seed',N'seed');

INSERT INTO Users (FirstName,LastName,Identification,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,LastLoginAt,Address,City,Country,OperatingLocation,IpAddress,Latitude,Longitude,LastLoginIp,IsSuperUser,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
SELECT v.FirstName,v.LastName,v.Identification,v.Gender,v.Email,@Hash,v.PhoneNumber,v.BirthDate,v.UserName,v.CompanyId,v.LegacyRole,NULL,v.Address,v.City,N'República Dominicana',v.OperatingLocation,NULL,v.Latitude,v.Longitude,NULL,0,@Now,1,0,NULL,N'seed',N'seed'
FROM (VALUES
 (N'Ana',N'Administrador',N'RBAC-A-001',N'F',N'hier.admin.a@cuadreenv.local',N'000-200-0001',CONVERT(date,'1988-01-15'),N'hier.admin.a',@CompanyA,N'Admin',N'Oficina principal',N'Santo Domingo',N'Sede administrativa',18.4861,-69.9312),
 (N'Luis',N'Administrador',N'RBAC-A-002',N'M',N'hier.admin.b@cuadreenv.local',N'000-200-0002',CONVERT(date,'1987-02-20'),N'hier.admin.b',@CompanyB,N'Admin',N'Oficina central',N'Santiago',N'Sede administrativa',19.4517,-70.6970),
 (N'Marta',N'Auditora',N'RBAC-A-003',N'F',N'hier.audit.a@cuadreenv.local',N'000-200-0003',CONVERT(date,'1990-03-10'),N'hier.audit.a',@CompanyA,N'Audit',N'Departamento de auditoría',N'Santo Domingo',N'Oficina de control',18.4861,-69.9312),
 (N'Pedro',N'Auditor',N'RBAC-A-004',N'M',N'hier.audit.b@cuadreenv.local',N'000-200-0004',CONVERT(date,'1991-04-12'),N'hier.audit.b',@CompanyB,N'Audit',N'Departamento de auditoría',N'Santiago',N'Oficina de control',19.4517,-70.6970),
 (N'Carla',N'Ventas',N'RBAC-A-005',N'F',N'hier.sales.a@cuadreenv.local',N'000-200-0005',CONVERT(date,'1993-05-25'),N'hier.sales.a',@CompanyA,N'Employee',N'Punto de ventas 1',N'Santo Domingo',N'Terminal POS 01',18.4861,-69.9312),
 (N'Jorge',N'Ventas',N'RBAC-A-006',N'M',N'hier.sales.b@cuadreenv.local',N'000-200-0006',CONVERT(date,'1992-06-18'),N'hier.sales.b',@CompanyB,N'Employee',N'Punto de ventas 1',N'Santiago',N'Terminal POS 01',19.4517,-70.6970),
 (N'Elena',N'Inventario',N'RBAC-A-007',N'F',N'hier.inventory.a@cuadreenv.local',N'000-200-0007',CONVERT(date,'1989-07-08'),N'hier.inventory.a',@CompanyA,N'Employee',N'Almacén principal',N'Santo Domingo',N'Terminal almacén 01',18.4861,-69.9312),
 (N'Rafael',N'Inventario',N'RBAC-A-008',N'M',N'hier.inventory.b@cuadreenv.local',N'000-200-0008',CONVERT(date,'1988-08-14'),N'hier.inventory.b',@CompanyB,N'Employee',N'Almacén principal',N'Santiago',N'Terminal almacén 01',19.4517,-70.6970),
 (N'Sofía',N'Operadora',N'RBAC-A-009',N'F',N'hier.employee.a@cuadreenv.local',N'000-200-0009',CONVERT(date,'1995-09-30'),N'hier.employee.a',@CompanyA,N'Employee',N'Caja principal',N'Santo Domingo',N'Terminal caja 01',18.4861,-69.9312),
 (N'Diego',N'Operador',N'RBAC-A-010',N'M',N'hier.employee.b@cuadreenv.local',N'000-200-0010',CONVERT(date,'1994-10-22'),N'hier.employee.b',@CompanyB,N'Employee',N'Caja principal',N'Santiago',N'Terminal caja 01',19.4517,-70.6970)
) AS v(FirstName,LastName,Identification,Gender,Email,PhoneNumber,BirthDate,UserName,CompanyId,LegacyRole,Address,City,OperatingLocation,Latitude,Longitude)
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email=v.Email);

DECLARE @UserId int, @RoleId int, @Email nvarchar(200), @CompanyId int, @RoleName nvarchar(100);
DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT u.Id,u.Email,u.CompanyId,
       CASE
         WHEN u.Email IN (N'hier.admin.a@cuadreenv.local',N'hier.admin.b@cuadreenv.local') THEN N'RBAC Admin'
         WHEN u.Email IN (N'hier.audit.a@cuadreenv.local',N'hier.audit.b@cuadreenv.local') THEN N'RBAC Audit'
         WHEN u.Email IN (N'hier.sales.a@cuadreenv.local',N'hier.sales.b@cuadreenv.local') THEN N'RBAC Sales Operator'
         WHEN u.Email IN (N'hier.inventory.a@cuadreenv.local',N'hier.inventory.b@cuadreenv.local') THEN N'RBAC Inventory Manager'
         ELSE N'RBAC Employee'
       END
FROM Users u WHERE u.Email LIKE N'hier.%@cuadreenv.local';
OPEN user_cursor;
FETCH NEXT FROM user_cursor INTO @UserId,@Email,@CompanyId,@RoleName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @RoleId=Id FROM Roles WHERE CompanyId=@CompanyId AND Name=@RoleName;
    IF @RoleId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId=@UserId AND RoleId=@RoleId)
        INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) VALUES (@UserId,@RoleId,@CompanyId,@Now,NULL);
    FETCH NEXT FROM user_cursor INTO @UserId,@Email,@CompanyId,@RoleName;
END;
CLOSE user_cursor;
DEALLOCATE user_cursor;

DECLARE @RoleCursorId int;
DECLARE role_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM Roles WHERE Name=N'RBAC Employee' AND CompanyId IN (@CompanyA,@CompanyB);
OPEN role_cursor;
FETCH NEXT FROM role_cursor INTO @RoleCursorId;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO RolePermissions(RoleId,PermissionId)
    SELECT @RoleCursorId,p.Id FROM Permissions p
    WHERE ((p.Module=N'Sales' AND p.Action=N'View') OR (p.Module=N'Customers' AND p.Action=N'View'))
      AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleCursorId AND x.PermissionId=p.Id);
    FETCH NEXT FROM role_cursor INTO @RoleCursorId;
END;
CLOSE role_cursor;
DEALLOCATE role_cursor;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM UserRoles WHERE UserId IN (SELECT Id FROM Users WHERE Email LIKE N'hier.%@cuadreenv.local');
DELETE FROM Users WHERE Email LIKE N'hier.%@cuadreenv.local';
DELETE FROM Roles WHERE Name=N'RBAC Employee' AND CompanyId IN (SELECT Id FROM Companies WHERE Rnc IN (N'RBAC-DEMO-A',N'RBAC-DEMO-B'));
");
        }
    }
}

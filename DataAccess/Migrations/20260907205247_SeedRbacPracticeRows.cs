using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedRbacPracticeRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @Hash nvarchar(max) = N'$2a$11$0vkPUpySVVVe36tEDS1lneBomhzRhTmbzzjT.shJokZJ/ajRXL.uq';
DECLARE @CompanyA int, @CompanyB int, @AdminA int, @AuditA int, @EmployeeA int, @AdminB int, @AuditB int, @EmployeeB int;

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Rnc = N'RBAC-DEMO-A')
    INSERT INTO Companies (Name,Rnc,Email,Address,Phone,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
    VALUES (N'RBAC Demo Empresa A',N'RBAC-DEMO-A',N'rbac.empresa.a@cuadreenv.local',N'Tenant A',N'000-100-0001',@Now,1,0,NULL,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Companies WHERE Rnc = N'RBAC-DEMO-B')
    INSERT INTO Companies (Name,Rnc,Email,Address,Phone,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
    VALUES (N'RBAC Demo Empresa B',N'RBAC-DEMO-B',N'rbac.empresa.b@cuadreenv.local',N'Tenant B',N'000-100-0002',@Now,1,0,NULL,N'seed',N'seed');
SELECT @CompanyA=Id FROM Companies WHERE Rnc=N'RBAC-DEMO-A';
SELECT @CompanyB=Id FROM Companies WHERE Rnc=N'RBAC-DEMO-B';

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Sales' AND Action=N'View') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Sales',N'View',N'Consultar ventas',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Sales' AND Action=N'Create') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Sales',N'Create',N'Crear ventas',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Inventory' AND Action=N'View') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Inventory',N'View',N'Consultar inventario',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Inventory' AND Action=N'Edit') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Inventory',N'Edit',N'Editar inventario',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Customers' AND Action=N'View') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Customers',N'View',N'Consultar clientes',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Audit' AND Action=N'View') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Audit',N'View',N'Consultar auditoría',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Audit' AND Action=N'Approve') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Audit',N'Approve',N'Aprobar eliminaciones',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Configuration' AND Action=N'Edit') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Configuration',N'Edit',N'Configurar roles',@Now,1,0,N'seed',N'seed');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Admin') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyA,N'RBAC Admin',N'Administrador del tenant',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Audit') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyA,N'RBAC Audit',N'Auditor con aprobación requerida',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Sales Operator') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyA,N'RBAC Sales Operator',N'Rol personalizado de ventas',0,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Admin') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyB,N'RBAC Admin',N'Administrador del tenant',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Audit') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyB,N'RBAC Audit',N'Auditor con aprobación requerida',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Inventory Manager') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyB,N'RBAC Inventory Manager',N'Rol personalizado de inventario',0,@Now,NULL,@Now,1,0,N'seed',N'seed');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.admin.a@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Admin A',N'M',N'rbac.admin.a@cuadreenv.local',@Hash,N'000-100-0010','1990-01-01',N'rbac.admin.a',@CompanyA,N'Admin',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.audit.a@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Audit A',N'M',N'rbac.audit.a@cuadreenv.local',@Hash,N'000-100-0011','1990-01-02',N'rbac.audit.a',@CompanyA,N'Audit',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.employee.a@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Employee A',N'M',N'rbac.employee.a@cuadreenv.local',@Hash,N'000-100-0012','1990-01-03',N'rbac.employee.a',@CompanyA,N'Employee',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.admin.b@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Admin B',N'M',N'rbac.admin.b@cuadreenv.local',@Hash,N'000-100-0020','1990-02-01',N'rbac.admin.b',@CompanyB,N'Admin',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.audit.b@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Audit B',N'M',N'rbac.audit.b@cuadreenv.local',@Hash,N'000-100-0021','1990-02-02',N'rbac.audit.b',@CompanyB,N'Audit',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.employee.b@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Employee B',N'M',N'rbac.employee.b@cuadreenv.local',@Hash,N'000-100-0022','1990-02-03',N'rbac.employee.b',@CompanyB,N'Employee',0,@Now,1,0,N'seed',N'seed');
SELECT @AdminA=Id FROM Users WHERE Email=N'rbac.admin.a@cuadreenv.local'; SELECT @AuditA=Id FROM Users WHERE Email=N'rbac.audit.a@cuadreenv.local'; SELECT @EmployeeA=Id FROM Users WHERE Email=N'rbac.employee.a@cuadreenv.local'; SELECT @AdminB=Id FROM Users WHERE Email=N'rbac.admin.b@cuadreenv.local'; SELECT @AuditB=Id FROM Users WHERE Email=N'rbac.audit.b@cuadreenv.local'; SELECT @EmployeeB=Id FROM Users WHERE Email=N'rbac.employee.b@cuadreenv.local';

INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @AdminA,r.Id,@CompanyA,@Now,@AdminA FROM Roles r WHERE r.CompanyId=@CompanyA AND r.Name=N'RBAC Admin' AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@AdminA AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @AuditA,r.Id,@CompanyA,@Now,@AdminA FROM Roles r WHERE r.CompanyId=@CompanyA AND r.Name=N'RBAC Audit' AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@AuditA AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @EmployeeA,r.Id,@CompanyA,@Now,@AdminA FROM Roles r WHERE r.CompanyId=@CompanyA AND r.Name IN (N'RBAC Sales Operator',N'RBAC Audit') AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@EmployeeA AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @AdminB,r.Id,@CompanyB,@Now,@AdminB FROM Roles r WHERE r.CompanyId=@CompanyB AND r.Name=N'RBAC Admin' AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@AdminB AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @AuditB,r.Id,@CompanyB,@Now,@AdminB FROM Roles r WHERE r.CompanyId=@CompanyB AND r.Name=N'RBAC Audit' AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@AuditB AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @EmployeeB,r.Id,@CompanyB,@Now,@AdminB FROM Roles r WHERE r.CompanyId=@CompanyB AND r.Name IN (N'RBAC Inventory Manager',N'RBAC Audit') AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@EmployeeB AND x.RoleId=r.Id);

INSERT INTO Products (Description,Barcode,ShortDescription,Reference,MaximumQuantity,MinimumQuantity,ProductTypeId,CategoryId,CompanyId,UnitOfMeasurementId,InvoiceWithoutStock,Cost,Stock,ReservedStock,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) SELECT N'Producto RBAC A',N'RBAC-A-001',N'Demo A',N'RBAC-A-001',100,5,1,1,@CompanyA,1,0,10,25,0,@Now,1,0,N'seed',N'seed' WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Barcode=N'RBAC-A-001');
INSERT INTO Products (Description,Barcode,ShortDescription,Reference,MaximumQuantity,MinimumQuantity,ProductTypeId,CategoryId,CompanyId,UnitOfMeasurementId,InvoiceWithoutStock,Cost,Stock,ReservedStock,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) SELECT N'Producto RBAC B',N'RBAC-B-001',N'Demo B',N'RBAC-B-001',100,5,1,1,@CompanyB,1,0,20,30,0,@Now,1,0,N'seed',N'seed' WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Barcode=N'RBAC-B-001');
DECLARE @ProductA int, @ProductB int; SELECT @ProductA=Id FROM Products WHERE Barcode=N'RBAC-A-001'; SELECT @ProductB=Id FROM Products WHERE Barcode=N'RBAC-B-001';
IF NOT EXISTS (SELECT 1 FROM DeletionApprovalRequests WHERE CompanyId=@CompanyA AND EntityType=N'Product' AND EntityId=@ProductA AND Status=N'Pending') INSERT INTO DeletionApprovalRequests (CompanyId,EntityType,EntityId,RequestedByUserId,RequestedAt,Reason,Status,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyA,N'Product',@ProductA,@AuditA,@Now,N'Validar aprobación de borrado en tenant A',N'Pending',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM DeletionApprovalRequests WHERE CompanyId=@CompanyB AND EntityType=N'Product' AND EntityId=@ProductB AND Status=N'Pending') INSERT INTO DeletionApprovalRequests (CompanyId,EntityType,EntityId,RequestedByUserId,RequestedAt,Reason,Status,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyB,N'Product',@ProductB,@AuditB,@Now,N'Validar aprobación de borrado en tenant B',N'Pending',@Now,1,0,N'seed',N'seed');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

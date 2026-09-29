using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedCuadreEnvDemoUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @CompanyId int;
DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @PasswordHash nvarchar(max) = N'$2a$11$0vkPUpySVVVe36tEDS1lneBomhzRhTmbzzjT.shJokZJ/ajRXL.uq';

SELECT @CompanyId = Id FROM Companies WHERE Email = N'demo@cuadreenv.local';

IF @CompanyId IS NULL
BEGIN
    INSERT INTO Companies (Name, Rnc, Email, Address, Phone, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'CuadreEnv Demo', N'DEMO-000000', N'demo@cuadreenv.local', N'Empresa de demostración', N'000-000-0000', @Now, 1, 0, NULL, N'migration', N'migration');
    SET @CompanyId = CONVERT(int, SCOPE_IDENTITY());
END;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.admin@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Admin', NULL, N'M', N'demo.admin@cuadreenv.local', @PasswordHash, N'000-000-0001', '1990-01-01', N'demo.admin', @CompanyId, N'Admin', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.supervisor@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Supervisor', NULL, N'M', N'demo.supervisor@cuadreenv.local', @PasswordHash, N'000-000-0002', '1990-01-02', N'demo.supervisor', @CompanyId, N'Supervisor', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.vendedor@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Vendedor', NULL, N'M', N'demo.vendedor@cuadreenv.local', @PasswordHash, N'000-000-0003', '1990-01-03', N'demo.vendedor', @CompanyId, N'Vendedor', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.cajero@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Cajero', NULL, N'M', N'demo.cajero@cuadreenv.local', @PasswordHash, N'000-000-0004', '1990-01-04', N'demo.cajero', @CompanyId, N'Cajero', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.employee@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Employee', NULL, N'M', N'demo.employee@cuadreenv.local', @PasswordHash, N'000-000-0005', '1990-01-05', N'demo.employee', @CompanyId, N'Employee', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @CompanyId int;
SELECT @CompanyId = Id FROM Companies WHERE Email = N'demo@cuadreenv.local';
DELETE FROM Users WHERE Email IN (N'demo.admin@cuadreenv.local', N'demo.supervisor@cuadreenv.local', N'demo.vendedor@cuadreenv.local', N'demo.cajero@cuadreenv.local', N'demo.employee@cuadreenv.local');
IF @CompanyId IS NOT NULL
BEGIN
    DELETE FROM CompanySettings WHERE CompanyId = @CompanyId;
    DELETE FROM Companies WHERE Id = @CompanyId;
END;
");
        }
    }
}

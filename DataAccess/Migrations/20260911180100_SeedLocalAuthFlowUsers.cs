using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <summary>
    /// Creates local authentication users for development and tenant-isolation testing.
    /// These accounts are intentionally not production credentials.
    /// </summary>
    public partial class SeedLocalAuthFlowUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @Now datetime2 = GETUTCDATE();
                DECLARE @PasswordHash nvarchar(500) = N'$2a$11$fwsUE3NQ7K5Rl69qFuRedOtzKiFEGOb8gUXQbxU20Dg4TfVcQhR0W';
                DECLARE @CompanyA int, @CompanyB int;

                SELECT @CompanyA = Id FROM Companies WHERE Rnc = N'LOCAL-AUTH-A';
                SELECT @CompanyB = Id FROM Companies WHERE Rnc = N'LOCAL-AUTH-B';

                IF @CompanyA IS NULL
                BEGIN
                    INSERT INTO Companies
                        (Name, Rnc, Email, Address, Phone, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
                    VALUES
                        (N'Local Auth Demo A', N'LOCAL-AUTH-A', N'local.auth.a@cuadreenv.local', N'Santo Domingo', N'000-300-0001', @Now, 1, 0, NULL, N'seed', N'seed');
                    SET @CompanyA = CONVERT(int, SCOPE_IDENTITY());
                END;

                IF @CompanyB IS NULL
                BEGIN
                    INSERT INTO Companies
                        (Name, Rnc, Email, Address, Phone, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
                    VALUES
                        (N'Local Auth Demo B', N'LOCAL-AUTH-B', N'local.auth.b@cuadreenv.local', N'Santiago', N'000-300-0002', @Now, 1, 0, NULL, N'seed', N'seed');
                    SET @CompanyB = CONVERT(int, SCOPE_IDENTITY());
                END;

                INSERT INTO Users
                    (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate,
                     UserName, CompanyId, Role, LastLoginAt, Address, City, Country, OperatingLocation,
                     IpAddress, Latitude, Longitude, LastLoginIp, IsSuperUser, CreationDate, Active,
                     IsDeleted, ModificationDate, CreateBy, ModifiedBy)
                SELECT v.FirstName, v.LastName, v.Identification, v.Gender, v.Email, @PasswordHash, v.PhoneNumber,
                       v.BirthDate, v.UserName, v.CompanyId, v.Role, NULL, v.Address, v.City, N'República Dominicana',
                       v.OperatingLocation, NULL, NULL, NULL, NULL, 0, @Now, 1, 0, NULL, N'seed', N'seed'
                FROM (VALUES
                    (N'Local', N'Admin A', N'LOCAL-AUTH-A-001', N'M', N'local.admin.a@cuadreenv.local', N'000-300-1001', CONVERT(date, '1990-01-01'), N'local.admin.a', @CompanyA, N'Admin', N'Oficina A', N'Santo Domingo', N'Administración'),
                    (N'Local', N'Auditor A', N'LOCAL-AUTH-A-002', N'F', N'local.audit.a@cuadreenv.local', N'000-300-1002', CONVERT(date, '1991-02-02'), N'local.audit.a', @CompanyA, N'Audit', N'Oficina A', N'Santo Domingo', N'Auditoría'),
                    (N'Local', N'Empleado A', N'LOCAL-AUTH-A-003', N'M', N'local.employee.a@cuadreenv.local', N'000-300-1003', CONVERT(date, '1992-03-03'), N'local.employee.a', @CompanyA, N'Employee', N'Sucursal A', N'Santo Domingo', N'Caja'),
                    (N'Local', N'Admin B', N'LOCAL-AUTH-B-001', N'F', N'local.admin.b@cuadreenv.local', N'000-300-2001', CONVERT(date, '1990-04-04'), N'local.admin.b', @CompanyB, N'Admin', N'Oficina B', N'Santiago', N'Administración'),
                    (N'Local', N'Auditor B', N'LOCAL-AUTH-B-002', N'M', N'local.audit.b@cuadreenv.local', N'000-300-2002', CONVERT(date, '1991-05-05'), N'local.audit.b', @CompanyB, N'Audit', N'Oficina B', N'Santiago', N'Auditoría'),
                    (N'Local', N'Empleado B', N'LOCAL-AUTH-B-003', N'F', N'local.employee.b@cuadreenv.local', N'000-300-2003', CONVERT(date, '1992-06-06'), N'local.employee.b', @CompanyB, N'Employee', N'Sucursal B', N'Santiago', N'Caja')
                ) AS v(FirstName, LastName, Identification, Gender, Email, PhoneNumber, BirthDate, UserName, CompanyId, Role, Address, City, OperatingLocation)
                WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email = v.Email);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM Users
                WHERE Email IN (
                    N'local.admin.a@cuadreenv.local', N'local.audit.a@cuadreenv.local', N'local.employee.a@cuadreenv.local',
                    N'local.admin.b@cuadreenv.local', N'local.audit.b@cuadreenv.local', N'local.employee.b@cuadreenv.local');
                """);
        }
    }
}

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.RefreshTokens
	(
		Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
		UserId int NOT NULL,
		Token nvarchar(max) NOT NULL,
		TokenHash nvarchar(max) NULL,
		Expires datetime2 NOT NULL,
		DeviceId nvarchar(max) NULL,
		LastUsedAt datetime2 NULL,
		IsRevoked bit NOT NULL,
		ReplacedByToken nvarchar(max) NULL,
		CreationDate datetime2 NOT NULL,
		Active bit NOT NULL,
		IsDeleted bit NOT NULL,
		ModificationDate datetime2 NULL,
		CreateBy nvarchar(max) NOT NULL,
		ModifiedBy nvarchar(max) NOT NULL,
		CONSTRAINT FK_RefreshTokens_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
	);
	CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens(UserId);
END
ELSE
BEGIN
	IF COL_LENGTH(N'dbo.RefreshTokens', N'TokenHash') IS NULL
		ALTER TABLE dbo.RefreshTokens ADD TokenHash nvarchar(max) NULL;
	IF COL_LENGTH(N'dbo.RefreshTokens', N'LastUsedAt') IS NULL
		ALTER TABLE dbo.RefreshTokens ADD LastUsedAt datetime2 NULL;
	IF COL_LENGTH(N'dbo.RefreshTokens', N'DeviceId') IS NULL
		ALTER TABLE dbo.RefreshTokens ADD DeviceId nvarchar(max) NULL;
	IF COL_LENGTH(N'dbo.RefreshTokens', N'ReplacedByToken') IS NULL
		ALTER TABLE dbo.RefreshTokens ADD ReplacedByToken nvarchar(max) NULL;
END;

DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @Hash nvarchar(500) = N'$2a$11$fwsUE3NQ7K5Rl69qFuRedOtzKiFEGOb8gUXQbxU20Dg4TfVcQhR0W';
DECLARE @CompanyA int, @CompanyB int;

SELECT @CompanyA = Id FROM Companies WHERE Rnc = N'LOCAL-AUTH-A';
IF @CompanyA IS NULL
BEGIN
	INSERT INTO Companies (Name, Rnc, Email, Address, Phone, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES (N'Local Auth Demo A', N'LOCAL-AUTH-A', N'local.auth.a@cuadreenv.local', N'Santo Domingo', N'000-300-0001', @Now, 1, 0, N'seed', N'seed');
	SET @CompanyA = CONVERT(int, SCOPE_IDENTITY());
END;

SELECT @CompanyB = Id FROM Companies WHERE Rnc = N'LOCAL-AUTH-B';
IF @CompanyB IS NULL
BEGIN
	INSERT INTO Companies (Name, Rnc, Email, Address, Phone, CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
	VALUES (N'Local Auth Demo B', N'LOCAL-AUTH-B', N'local.auth.b@cuadreenv.local', N'Santiago', N'000-300-0002', @Now, 1, 0, N'seed', N'seed');
	SET @CompanyB = CONVERT(int, SCOPE_IDENTITY());
END;

INSERT INTO Users
	(FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName,
	 CompanyId, Role, LastLoginAt, Address, City, Country, OperatingLocation, IsSuperUser,
	 CreationDate, Active, IsDeleted, CreateBy, ModifiedBy)
SELECT v.FirstName, v.LastName, v.Identification, v.Gender, v.Email, @Hash, v.PhoneNumber, v.BirthDate,
	   v.UserName, v.CompanyId, v.Role, NULL, v.Address, v.City, N'República Dominicana', v.OperatingLocation,
	   0, @Now, 1, 0, N'seed', N'seed'
FROM (VALUES
	(N'Local', N'Admin A', N'LOCAL-AUTH-A-001', N'M', N'local.admin.a@cuadreenv.local', N'000-300-1001', CONVERT(date, '1990-01-01'), N'local.admin.a', @CompanyA, N'Admin', N'Oficina A', N'Santo Domingo', N'Administración'),
	(N'Local', N'Auditor A', N'LOCAL-AUTH-A-002', N'F', N'local.audit.a@cuadreenv.local', N'000-300-1002', CONVERT(date, '1991-02-02'), N'local.audit.a', @CompanyA, N'Audit', N'Oficina A', N'Santo Domingo', N'Auditoría'),
	(N'Local', N'Empleado A', N'LOCAL-AUTH-A-003', N'M', N'local.employee.a@cuadreenv.local', N'000-300-1003', CONVERT(date, '1992-03-03'), N'local.employee.a', @CompanyA, N'Employee', N'Sucursal A', N'Santo Domingo', N'Caja'),
	(N'Local', N'Admin B', N'LOCAL-AUTH-B-001', N'F', N'local.admin.b@cuadreenv.local', N'000-300-2001', CONVERT(date, '1990-04-04'), N'local.admin.b', @CompanyB, N'Admin', N'Oficina B', N'Santiago', N'Administración'),
	(N'Local', N'Auditor B', N'LOCAL-AUTH-B-002', N'M', N'local.audit.b@cuadreenv.local', N'000-300-2002', CONVERT(date, '1991-05-05'), N'local.audit.b', @CompanyB, N'Audit', N'Oficina B', N'Santiago', N'Auditoría'),
	(N'Local', N'Empleado B', N'LOCAL-AUTH-B-003', N'F', N'local.employee.b@cuadreenv.local', N'000-300-2003', CONVERT(date, '1992-06-06'), N'local.employee.b', @CompanyB, N'Employee', N'Sucursal B', N'Santiago', N'Caja')
) AS v(FirstName, LastName, Identification, Gender, Email, PhoneNumber, BirthDate, UserName, CompanyId, Role, Address, City, OperatingLocation)
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email = v.Email);

COMMIT;

SELECT DB_NAME() AS DatabaseName,
	   COL_LENGTH(N'dbo.RefreshTokens', N'TokenHash') AS TokenHashLength,
	   COL_LENGTH(N'dbo.RefreshTokens', N'LastUsedAt') AS LastUsedAtLength;
SELECT u.Email, u.Role, u.CompanyId, c.Name AS CompanyName
FROM Users u
JOIN Companies c ON c.Id = u.CompanyId
WHERE u.Email LIKE N'local.%@cuadreenv.local'
ORDER BY u.CompanyId, u.Email;

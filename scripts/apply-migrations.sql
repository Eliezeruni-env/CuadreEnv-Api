-- Apply DB changes for IsSuperUser and AuditLogs
-- Run this against the OnionCrud database (localdb) using sqlcmd or SSMS.

IF COL_LENGTH('dbo.Users','IsSuperUser') IS NULL
BEGIN
	PRINT 'Adding IsSuperUser column to dbo.Users';
	ALTER TABLE dbo.Users ADD IsSuperUser bit NOT NULL CONSTRAINT DF_Users_IsSuperUser DEFAULT (0);
END
ELSE
BEGIN
	PRINT 'Column IsSuperUser already exists';
END

IF OBJECT_ID('dbo.AuditLogs','U') IS NULL
BEGIN
	PRINT 'Creating table dbo.AuditLogs';
	CREATE TABLE dbo.AuditLogs (
		Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
		Action nvarchar(max) NOT NULL,
		Entity nvarchar(max) NOT NULL,
		EntityId int NULL,
		PerformedBy nvarchar(max) NOT NULL,
		Details nvarchar(max) NULL,
		CreationDate datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
		ModificationDate datetime2 NULL,
		CreateBy nvarchar(max) NOT NULL DEFAULT ('system'),
		ModifiedBy nvarchar(max) NOT NULL DEFAULT ('system')
	);
END
ELSE
BEGIN
	PRINT 'Table AuditLogs already exists';
END

IF OBJECT_ID('dbo.__EFMigrationsHistory','U') IS NOT NULL
BEGIN
	IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20260915120000_AddIsSuperUser')
		INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260915120000_AddIsSuperUser', '9.0.0');
	IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20260915121000_CreateAuditLog')
		INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260915121000_CreateAuditLog', '9.0.0');
END

PRINT 'Finished applying DB updates.';

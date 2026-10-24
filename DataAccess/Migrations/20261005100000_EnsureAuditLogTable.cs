using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Onion.DataAccess;

#nullable disable

namespace Onion.DataAccess.Migrations;

[Migration("20261005100000_EnsureAuditLogTable")]
[DbContext(typeof(OnionDbContext))]
public partial class EnsureAuditLogTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[AuditLogs]
                (
                    [Id] int NOT NULL IDENTITY(1, 1),
                    [Action] nvarchar(max) NOT NULL,
                    [Entity] nvarchar(max) NOT NULL,
                    [EntityId] int NULL,
                    [PerformedBy] nvarchar(max) NOT NULL,
                    [Details] nvarchar(max) NULL,
                    [CreationDate] datetime2 NOT NULL,
                    [ModificationDate] datetime2 NULL,
                    [CreateBy] nvarchar(max) NOT NULL,
                    [ModifiedBy] nvarchar(max) NOT NULL,
                    [Active] bit NOT NULL CONSTRAINT [DF_AuditLogs_Active] DEFAULT (0),
                    [IsDeleted] bit NOT NULL CONSTRAINT [DF_AuditLogs_IsDeleted] DEFAULT (0),
                    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
                );
            END

            IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.AuditLogs', N'AuditPayload') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [AuditPayload] nvarchar(max) NULL;
                IF COL_LENGTH(N'dbo.AuditLogs', N'CompanyId') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [CompanyId] int NULL;
                IF COL_LENGTH(N'dbo.AuditLogs', N'EntityName') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [EntityName] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_EntityName] DEFAULT (N'');
                IF COL_LENGTH(N'dbo.AuditLogs', N'IpAddress') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [IpAddress] nvarchar(max) NULL;
                IF COL_LENGTH(N'dbo.AuditLogs', N'Timestamp') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [Timestamp] datetime2 NOT NULL CONSTRAINT [DF_AuditLogs_Timestamp] DEFAULT ('0001-01-01T00:00:00');
                IF COL_LENGTH(N'dbo.AuditLogs', N'UserAgent') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [UserAgent] nvarchar(max) NULL;
                IF COL_LENGTH(N'dbo.AuditLogs', N'UserEmail') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [UserEmail] nvarchar(max) NULL;
                IF COL_LENGTH(N'dbo.AuditLogs', N'UserId') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [UserId] int NULL;
                IF COL_LENGTH(N'dbo.AuditLogs', N'UserRole') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [UserRole] nvarchar(max) NULL;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL
                DROP TABLE [dbo].[AuditLogs];
            """);
    }
}

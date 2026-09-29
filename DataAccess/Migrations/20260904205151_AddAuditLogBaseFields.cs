using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogBaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.AuditLogs', N'Active') IS NULL
                        ALTER TABLE [dbo].[AuditLogs] ADD [Active] bit NOT NULL CONSTRAINT [DF_AuditLogs_Active] DEFAULT (0);
                    IF COL_LENGTH(N'dbo.AuditLogs', N'IsDeleted') IS NULL
                        ALTER TABLE [dbo].[AuditLogs] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_AuditLogs_IsDeleted] DEFAULT (0);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.AuditLogs', N'Active') IS NOT NULL
                        ALTER TABLE [dbo].[AuditLogs] DROP CONSTRAINT IF EXISTS [DF_AuditLogs_Active];
                    IF COL_LENGTH(N'dbo.AuditLogs', N'Active') IS NOT NULL
                        ALTER TABLE [dbo].[AuditLogs] DROP COLUMN [Active];
                    IF COL_LENGTH(N'dbo.AuditLogs', N'IsDeleted') IS NOT NULL
                        ALTER TABLE [dbo].[AuditLogs] DROP CONSTRAINT IF EXISTS [DF_AuditLogs_IsDeleted];
                    IF COL_LENGTH(N'dbo.AuditLogs', N'IsDeleted') IS NOT NULL
                        ALTER TABLE [dbo].[AuditLogs] DROP COLUMN [IsDeleted];
                END
                """);
        }
    }
}

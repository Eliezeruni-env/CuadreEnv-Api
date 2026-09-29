using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    [Migration("20260915121000_CreateAuditLog")]
    public partial class CreateAuditLog : Migration
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
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL DROP TABLE [dbo].[AuditLogs];");
        }
    }
}

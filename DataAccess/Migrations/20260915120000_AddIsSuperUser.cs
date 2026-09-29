using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    [Migration("20260915120000_AddIsSuperUser")]
    public partial class AddIsSuperUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.Users', N'IsSuperUser') IS NULL
                    ALTER TABLE [dbo].[Users] ADD [IsSuperUser] bit NOT NULL CONSTRAINT [DF_Users_IsSuperUser] DEFAULT (0);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.Users', N'IsSuperUser') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Users] DROP CONSTRAINT IF EXISTS [DF_Users_IsSuperUser];
                    ALTER TABLE [dbo].[Users] DROP COLUMN [IsSuperUser];
                END
                """);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NombreMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.Users', N'IsSuperUser') IS NULL
                    ALTER TABLE [dbo].[Users] ADD [IsSuperUser] bit NOT NULL CONSTRAINT [DF_Users_IsSuperUser] DEFAULT (0);
                """);
        }

        /// <inheritdoc />
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

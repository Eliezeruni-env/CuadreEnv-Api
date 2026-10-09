using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <summary>
    /// Repairs databases where refresh-token columns were present in the EF model
    /// but absent from the physical RefreshTokens table.
    /// </summary>
    public partial class RepairRefreshTokenColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[RefreshTokens]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.RefreshTokens', N'TokenHash') IS NULL
                        ALTER TABLE [dbo].[RefreshTokens] ADD [TokenHash] nvarchar(max) NULL;

                    IF COL_LENGTH(N'dbo.RefreshTokens', N'LastUsedAt') IS NULL
                        ALTER TABLE [dbo].[RefreshTokens] ADD [LastUsedAt] datetime2 NULL;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately non-destructive: this repair must not remove columns or data
            // from an existing database when a migration is rolled back.
        }
    }
}

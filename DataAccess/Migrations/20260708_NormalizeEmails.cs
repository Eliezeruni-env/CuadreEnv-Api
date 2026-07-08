using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    public partial class NormalizeEmails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize existing email values to trim whitespace and lowercase
            migrationBuilder.Sql(@"UPDATE [Users] SET [Email] = LOWER(LTRIM(RTRIM([Email]))); UPDATE [Users] SET [UserName] = LOWER(LTRIM(RTRIM([UserName])));");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No reliable down migration for normalized values; leave as-is
        }
    }
}

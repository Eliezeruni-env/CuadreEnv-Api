using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    public partial class AddRefreshTokenHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "RefreshTokens");
        }
    }
}

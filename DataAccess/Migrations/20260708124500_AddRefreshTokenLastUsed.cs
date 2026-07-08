using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    public partial class AddRefreshTokenLastUsed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "RefreshTokens");
        }
    }
}

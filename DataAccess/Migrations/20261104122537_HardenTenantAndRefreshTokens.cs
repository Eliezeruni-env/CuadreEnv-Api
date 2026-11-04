using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HardenTenantAndRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "RefreshTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"RefreshTokens\" rt SET \"CompanyId\" = u.\"CompanyId\" FROM \"Users\" u WHERE rt.\"UserId\" = u.\"Id\" AND u.\"CompanyId\" IS NOT NULL");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIn",
                table: "CashSessions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOut",
                table: "CashSessions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_CompanyId_TokenHash",
                table: "RefreshTokens",
                columns: new[] { "UserId", "CompanyId", "TokenHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_CompanyId_TokenHash",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "TotalIn",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "TotalOut",
                table: "CashSessions");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalRetryScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_CompanyId_Status_LastAttemptAt",
                table: "FiscalDocuments");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "FiscalDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_CompanyId_Status_NextAttemptAt",
                table: "FiscalDocuments",
                columns: new[] { "CompanyId", "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FiscalDocuments_CompanyId_Status_NextAttemptAt",
                table: "FiscalDocuments");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "FiscalDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_CompanyId_Status_LastAttemptAt",
                table: "FiscalDocuments",
                columns: new[] { "CompanyId", "Status", "LastAttemptAt" });
        }
    }
}

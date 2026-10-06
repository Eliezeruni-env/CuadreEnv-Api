using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalSequencesCashSessionsAndTaxpayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LegalTip",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxWithheld",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VoucherType",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AllowNegativeStock",
                table: "CompanySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CashToleranceAmount",
                table: "CompanySettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CashSessionId",
                table: "CashMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CashMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CashSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CashierUserId = table.Column<int>(type: "int", nullable: false),
                    CashRegisterId = table.Column<int>(type: "int", nullable: false),
                    OpeningAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpeningDenominationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeclaredCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeclaredCards = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeclaredTransfers = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ClosingStatus = table.Column<int>(type: "int", nullable: true),
                    ClosingDenominationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupervisorUserId = table.Column<int>(type: "int", nullable: true),
                    SupervisorNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VoucherType = table.Column<int>(type: "int", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CurrentNumber = table.Column<long>(type: "bigint", nullable: false),
                    FromNumber = table.Column<long>(type: "bigint", nullable: false),
                    ToNumber = table.Column<long>(type: "bigint", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarningThreshold = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Taxpayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RncOrCedula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CommercialName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastSynchronizedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taxpayers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_CompanyId_OpenedByUserId",
                table: "CashRegisters",
                columns: new[] { "CompanyId", "OpenedByUserId" },
                unique: true,
                filter: "[Status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CompanyId_CashSessionId_RecordedAt",
                table: "CashMovements",
                columns: new[] { "CompanyId", "CashSessionId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_CompanyId_CashierUserId",
                table: "CashSessions",
                columns: new[] { "CompanyId", "CashierUserId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_CompanyId_CashRegisterId",
                table: "CashSessions",
                columns: new[] { "CompanyId", "CashRegisterId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalSequences_CompanyId_IsActive_ExpirationDate",
                table: "FiscalSequences",
                columns: new[] { "CompanyId", "IsActive", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalSequences_CompanyId_VoucherType",
                table: "FiscalSequences",
                columns: new[] { "CompanyId", "VoucherType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Taxpayers_RncOrCedula",
                table: "Taxpayers",
                column: "RncOrCedula",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashSessions");

            migrationBuilder.DropTable(
                name: "FiscalSequences");

            migrationBuilder.DropTable(
                name: "Taxpayers");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_CompanyId_OpenedByUserId",
                table: "CashRegisters");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CompanyId_CashSessionId_RecordedAt",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "LegalTip",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TaxWithheld",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "VoucherType",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "AllowNegativeStock",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "CashToleranceAmount",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "CashSessionId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CashMovements");
        }
    }
}

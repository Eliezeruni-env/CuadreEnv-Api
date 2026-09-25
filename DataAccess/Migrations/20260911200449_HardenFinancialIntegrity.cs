using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HardenFinancialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InvoiceSequences",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAmount",
                table: "CashRegisters",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImmutable",
                table: "CashRegisters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PhysicalCountAmount",
                table: "CashRegisters",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicalCountBreakdownJson",
                table: "CashRegisters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CashRegisters",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "CashMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "CashMovements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordedAt",
                table: "CashMovements",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CashMovements",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "FiscalDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SaleId = table.Column<int>(type: "int", nullable: false),
                    DocumentKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ncf = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EcfTrackId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalSubmissionAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FiscalDocumentId = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalSubmissionAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Stock_MinimumQuantity",
                table: "Products",
                columns: new[] { "CompanyId", "Stock", "MinimumQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSequences_CompanyId",
                table: "InvoiceSequences",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_CompanyId_Status",
                table: "CashRegisters",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CompanyId_CashRegisterId_RecordedAt",
                table: "CashMovements",
                columns: new[] { "CompanyId", "CashRegisterId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_CompanyId_DocumentKey",
                table: "FiscalDocuments",
                columns: new[] { "CompanyId", "DocumentKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_CompanyId_Status_LastAttemptAt",
                table: "FiscalDocuments",
                columns: new[] { "CompanyId", "Status", "LastAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalSubmissionAudits_CompanyId_FiscalDocumentId_OccurredAt",
                table: "FiscalSubmissionAudits",
                columns: new[] { "CompanyId", "FiscalDocumentId", "OccurredAt" });

            migrationBuilder.Sql("""
                CREATE TRIGGER dbo.TR_CashRegisters_ImmutableAfterClose
                ON dbo.CashRegisters
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted WHERE IsImmutable = 1)
                        THROW 51001, 'Closed cash registers are immutable.', 1;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER dbo.TR_CashMovements_AppendOnly
                ON dbo.CashMovements
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51002, 'Cash movements are append-only.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_CashRegisters_ImmutableAfterClose;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_CashMovements_AppendOnly;");

            migrationBuilder.DropTable(
                name: "FiscalDocuments");

            migrationBuilder.DropTable(
                name: "FiscalSubmissionAudits");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_Stock_MinimumQuantity",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceSequences_CompanyId",
                table: "InvoiceSequences");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_CompanyId_Status",
                table: "CashRegisters");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CompanyId_CashRegisterId_RecordedAt",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InvoiceSequences");

            migrationBuilder.DropColumn(
                name: "ExpectedAmount",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "IsImmutable",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "PhysicalCountAmount",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "PhysicalCountBreakdownJson",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "RecordedAt",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CashMovements");
        }
    }
}

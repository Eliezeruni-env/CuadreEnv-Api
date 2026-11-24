using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HardenIntegrityAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MovementNumber",
                table: "Movements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "AccountReceivables",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PaidAmount",
                table: "AccountReceivables",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateIndex(
                name: "IX_Movements_CompanyId_CreationDate",
                table: "Movements",
                columns: new[] { "CompanyId", "CreationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Movements_CompanyId_MovementNumber",
                table: "Movements",
                columns: new[] { "CompanyId", "MovementNumber" },
                unique: true,
                filter: "\"MovementNumber\" IS NOT NULL AND \"MovementNumber\" <> '' AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AccountReceivables_CompanyId_SaleId",
                table: "AccountReceivables",
                columns: new[] { "CompanyId", "SaleId" },
                unique: true,
                filter: "\"SaleId\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movements_CompanyId_CreationDate",
                table: "Movements");

            migrationBuilder.DropIndex(
                name: "IX_Movements_CompanyId_MovementNumber",
                table: "Movements");

            migrationBuilder.DropIndex(
                name: "IX_AccountReceivables_CompanyId_SaleId",
                table: "AccountReceivables");

            migrationBuilder.AlterColumn<string>(
                name: "MovementNumber",
                table: "Movements",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "AccountReceivables",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PaidAmount",
                table: "AccountReceivables",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}

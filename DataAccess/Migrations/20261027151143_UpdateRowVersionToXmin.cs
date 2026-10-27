using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRowVersionToXmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "Sales",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "Products",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "InvoiceSequences",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "FiscalSequences",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "CashSessions",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "CashRegisters",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "CashMovements",
                newName: "xmin");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Sales",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Products",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "InvoiceSequences",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "FiscalSequences",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "CashSessions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "CashRegisters",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "CashMovements",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Sales",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Products",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "InvoiceSequences",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "FiscalSequences",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "CashSessions",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "CashRegisters",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "CashMovements",
                newName: "RowVersion");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Sales",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "InvoiceSequences",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "FiscalSequences",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "CashSessions",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "CashRegisters",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "CashMovements",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);
        }
    }
}

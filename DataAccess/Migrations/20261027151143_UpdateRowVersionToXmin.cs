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
            // xmin is a PostgreSQL system column. The legacy RowVersion columns
            // must be removed; they cannot be renamed to or altered into xmin.
            migrationBuilder.DropColumn(name: "RowVersion", table: "Sales");
            migrationBuilder.DropColumn(name: "RowVersion", table: "Products");
            migrationBuilder.DropColumn(name: "RowVersion", table: "InvoiceSequences");
            migrationBuilder.DropColumn(name: "RowVersion", table: "FiscalSequences");
            migrationBuilder.DropColumn(name: "RowVersion", table: "CashSessions");
            migrationBuilder.DropColumn(name: "RowVersion", table: "CashRegisters");
            migrationBuilder.DropColumn(name: "RowVersion", table: "CashMovements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sales",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "Products", type: "bytea", nullable: false);
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "InvoiceSequences", type: "bytea", nullable: false);
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "FiscalSequences", type: "bytea", nullable: false);
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "CashSessions", type: "bytea", nullable: false);
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "CashRegisters", type: "bytea", nullable: false);
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "CashMovements", type: "bytea", nullable: false);
        }
    }
}

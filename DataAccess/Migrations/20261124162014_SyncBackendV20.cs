using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SyncBackendV20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditNoteStatus",
                table: "Sales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasPartialCreditNote",
                table: "Sales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditedQuantity",
                table: "SaleDetails",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalQuantity",
                table: "SaleDetails",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Rnc",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReturnToStock",
                table: "CreditNotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledDate",
                table: "AccountReceivables",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditNoteStatus",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "HasPartialCreditNote",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CreditedQuantity",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "OriginalQuantity",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "Rnc",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ReturnToStock",
                table: "CreditNotes");

            migrationBuilder.DropColumn(
                name: "SettledDate",
                table: "AccountReceivables");
        }
    }
}

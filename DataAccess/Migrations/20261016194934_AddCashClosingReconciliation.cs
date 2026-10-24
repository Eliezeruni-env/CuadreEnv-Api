using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCashClosingReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualAmount",
                table: "CashSessions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardBatchNumber",
                table: "CashSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashToDeposit",
                table: "CashSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ClosingNotes",
                table: "CashSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DenominationsJson",
                table: "CashSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositDestination",
                table: "CashSessions",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAmount",
                table: "CashSessions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RetainedFloat",
                table: "CashSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SupervisorAuthorized",
                table: "CashSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TransferVoucherCount",
                table: "CashSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualAmount",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "CardBatchNumber",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "CashToDeposit",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "ClosingNotes",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "DenominationsJson",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "DepositDestination",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "ExpectedAmount",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "RetainedFloat",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "SupervisorAuthorized",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "TransferVoucherCount",
                table: "CashSessions");
        }
    }
}

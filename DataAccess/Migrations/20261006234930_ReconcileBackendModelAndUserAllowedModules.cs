using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileBackendModelAndUserAllowedModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ProductTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.AddColumn<string>(
                name: "AllowedModulesJson",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceCost",
                table: "InventoryMovements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceStock",
                table: "InventoryMovements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostUnit",
                table: "InventoryMovements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProcessedRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedRequests_CompanyId_IdempotencyKey_RequestPath",
                table: "ProcessedRequests",
                columns: new[] { "CompanyId", "IdempotencyKey", "RequestPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedRequests_ExpiresAtUtc",
                table: "ProcessedRequests",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedRequests");

            migrationBuilder.DropColumn(
                name: "AllowedModulesJson",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BalanceCost",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "BalanceStock",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "CostUnit",
                table: "InventoryMovements");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Active", "CompanyId", "CreateBy", "CreationDate", "Description", "IsDeleted", "ModificationDate", "ModifiedBy" },
                values: new object[,]
                {
                    { 1, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "General", false, null, "system" },
                    { 2, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Alimentos", false, null, "system" },
                    { 3, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bebidas", false, null, "system" },
                    { 4, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Papeler�a", false, null, "system" },
                    { 5, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Servicios", false, null, "system" }
                });

            migrationBuilder.InsertData(
                table: "ProductTypes",
                columns: new[] { "Id", "Active", "CompanyId", "CreateBy", "CreationDate", "Description", "IsDeleted", "ModificationDate", "ModifiedBy" },
                values: new object[,]
                {
                    { 1, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Producto Est�ndar", false, null, "system" },
                    { 2, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Servicio", false, null, "system" },
                    { 3, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Digital", false, null, "system" },
                    { 4, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Combo", false, null, "system" },
                    { 5, true, 0, "system", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Materia Prima", false, null, "system" }
                });
        }
    }
}

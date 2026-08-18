using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditsAppointmentsForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Resources_CompanyId",
                table: "Resources",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditStatusHistory_CompanyId",
                table: "CreditStatusHistory",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditStatusHistory_CreditId",
                table: "CreditStatusHistory",
                column: "CreditId");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_CompanyId",
                table: "Credits",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditPayments_CompanyId",
                table: "CreditPayments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditPayments_CreditId",
                table: "CreditPayments",
                column: "CreditId");

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_CompanyId",
                table: "Availabilities",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_ResourceId",
                table: "Availabilities",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CompanyId",
                table: "Appointments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ResourceId",
                table: "Appointments",
                column: "ResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Resources_ResourceId",
                table: "Appointments",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Availabilities_Resources_ResourceId",
                table: "Availabilities",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditPayments_Credits_CreditId",
                table: "CreditPayments",
                column: "CreditId",
                principalTable: "Credits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditStatusHistory_Credits_CreditId",
                table: "CreditStatusHistory",
                column: "CreditId",
                principalTable: "Credits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Resources_ResourceId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Availabilities_Resources_ResourceId",
                table: "Availabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditPayments_Credits_CreditId",
                table: "CreditPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditStatusHistory_Credits_CreditId",
                table: "CreditStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Resources_CompanyId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_CreditStatusHistory_CompanyId",
                table: "CreditStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_CreditStatusHistory_CreditId",
                table: "CreditStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Credits_CompanyId",
                table: "Credits");

            migrationBuilder.DropIndex(
                name: "IX_CreditPayments_CompanyId",
                table: "CreditPayments");

            migrationBuilder.DropIndex(
                name: "IX_CreditPayments_CreditId",
                table: "CreditPayments");

            migrationBuilder.DropIndex(
                name: "IX_Availabilities_CompanyId",
                table: "Availabilities");

            migrationBuilder.DropIndex(
                name: "IX_Availabilities_ResourceId",
                table: "Availabilities");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CompanyId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ResourceId",
                table: "Appointments");
        }
    }
}

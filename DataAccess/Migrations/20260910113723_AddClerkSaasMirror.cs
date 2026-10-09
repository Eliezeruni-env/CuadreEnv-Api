using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddClerkSaasMirror : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClerkOrganizations",
                columns: table => new
                {
                    ClerkOrganizationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClerkOrganizations", x => x.ClerkOrganizationId);
                });

            migrationBuilder.CreateTable(
                name: "ClerkUsers",
                columns: table => new
                {
                    ClerkUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClerkUsers", x => x.ClerkUserId);
                });

            migrationBuilder.CreateTable(
                name: "Proyectos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proyectos_ClerkOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "ClerkOrganizations",
                        principalColumn: "ClerkOrganizationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClerkOrganizationMembers",
                columns: table => new
                {
                    ClerkUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClerkOrganizationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClerkOrganizationMembers", x => new { x.ClerkUserId, x.ClerkOrganizationId });
                    table.ForeignKey(
                        name: "FK_ClerkOrganizationMembers_ClerkOrganizations_ClerkOrganizationId",
                        column: x => x.ClerkOrganizationId,
                        principalTable: "ClerkOrganizations",
                        principalColumn: "ClerkOrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClerkOrganizationMembers_ClerkUsers_ClerkUserId",
                        column: x => x.ClerkUserId,
                        principalTable: "ClerkUsers",
                        principalColumn: "ClerkUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClerkOrganizationMembers_ClerkOrganizationId",
                table: "ClerkOrganizationMembers",
                column: "ClerkOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_OrganizationId",
                table: "Proyectos",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClerkOrganizationMembers");

            migrationBuilder.DropTable(
                name: "Proyectos");

            migrationBuilder.DropTable(
                name: "ClerkUsers");

            migrationBuilder.DropTable(
                name: "ClerkOrganizations");
        }
    }
}

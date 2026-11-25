using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Onion.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCuadreEnvAgendaModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movements_CompanyId_MovementNumber",
                table: "Movements");

            migrationBuilder.AddColumn<int>(
                name: "AppointmentServiceId",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingStatus",
                table: "Appointments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BookingCode",
                table: "Appointments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Appointments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Appointments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "Appointments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagementTokenExpiresAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagementTokenHash",
                table: "Appointments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "Appointments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProfessionalId",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleId",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppointmentNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadPreview = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentNotifications_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentProfessionals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SpecialtyTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsOwner = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentProfessionals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentProfessionals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentRescheduleRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    ProposedStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProposedEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentRescheduleRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentRescheduleRequests_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    BufferTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsPubliclyBookable = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentServices_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfirmationMode = table.Column<int>(type: "integer", nullable: false),
                    DefaultDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinLeadTimeHours = table.Column<int>(type: "integer", nullable: false),
                    MaxFutureBookingDays = table.Column<int>(type: "integer", nullable: false),
                    HoldSlotTimeoutMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinCancelLeadTimeHours = table.Column<int>(type: "integer", nullable: false),
                    SlotIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    SendReminder24h = table.Column<bool>(type: "boolean", nullable: false),
                    SendReminder1h = table.Column<bool>(type: "boolean", nullable: false),
                    ChannelWhatsApp = table.Column<bool>(type: "boolean", nullable: false),
                    ChannelEmail = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentStatusHistories_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingPageSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BrandColorHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    WhatsAppNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsPublicActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPageSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentAvailabilityExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    ProfessionalId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    IsAllDay = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentAvailabilityExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentAvailabilityExceptions_AppointmentProfessionals_~",
                        column: x => x.ProfessionalId,
                        principalTable: "AppointmentProfessionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentEmployeeAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    ProfessionalId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BreakStartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    BreakEndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    IsWorkingDay = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentEmployeeAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentEmployeeAvailabilities_AppointmentProfessionals_~",
                        column: x => x.ProfessionalId,
                        principalTable: "AppointmentProfessionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentProfessionalServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    ProfessionalId = table.Column<int>(type: "integer", nullable: false),
                    AppointmentServiceId = table.Column<int>(type: "integer", nullable: false),
                    CustomDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    CustomPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentProfessionalServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentProfessionalServices_AppointmentProfessionals_Pr~",
                        column: x => x.ProfessionalId,
                        principalTable: "AppointmentProfessionals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentProfessionalServices_AppointmentServices_Appoint~",
                        column: x => x.AppointmentServiceId,
                        principalTable: "AppointmentServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movements_CompanyId_MovementNumber",
                table: "Movements",
                columns: new[] { "CompanyId", "MovementNumber" },
                filter: "\"MovementNumber\" IS NOT NULL AND \"MovementNumber\" <> '' AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentServiceId",
                table: "Appointments",
                column: "AppointmentServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CompanyId_BookingCode",
                table: "Appointments",
                columns: new[] { "CompanyId", "BookingCode" },
                unique: true,
                filter: "\"BookingCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CompanyId_ProfessionalId_StartAt_EndAt",
                table: "Appointments",
                columns: new[] { "CompanyId", "ProfessionalId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerId",
                table: "Appointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ManagementTokenHash",
                table: "Appointments",
                column: "ManagementTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ProfessionalId",
                table: "Appointments",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_SaleId",
                table: "Appointments",
                column: "SaleId",
                unique: true,
                filter: "\"SaleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAvailabilityExceptions_CompanyId",
                table: "AppointmentAvailabilityExceptions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAvailabilityExceptions_CompanyId_ProfessionalId_~",
                table: "AppointmentAvailabilityExceptions",
                columns: new[] { "CompanyId", "ProfessionalId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAvailabilityExceptions_ProfessionalId",
                table: "AppointmentAvailabilityExceptions",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentEmployeeAvailabilities_CompanyId",
                table: "AppointmentEmployeeAvailabilities",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentEmployeeAvailabilities_CompanyId_ProfessionalId_~",
                table: "AppointmentEmployeeAvailabilities",
                columns: new[] { "CompanyId", "ProfessionalId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentEmployeeAvailabilities_ProfessionalId",
                table: "AppointmentEmployeeAvailabilities",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotifications_AppointmentId",
                table: "AppointmentNotifications",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotifications_CompanyId",
                table: "AppointmentNotifications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProfessionals_CompanyId",
                table: "AppointmentProfessionals",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProfessionals_UserId",
                table: "AppointmentProfessionals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProfessionalServices_AppointmentServiceId",
                table: "AppointmentProfessionalServices",
                column: "AppointmentServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProfessionalServices_CompanyId",
                table: "AppointmentProfessionalServices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProfessionalServices_CompanyId_ProfessionalId_Ap~",
                table: "AppointmentProfessionalServices",
                columns: new[] { "CompanyId", "ProfessionalId", "AppointmentServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProfessionalServices_ProfessionalId",
                table: "AppointmentProfessionalServices",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRescheduleRequests_AppointmentId",
                table: "AppointmentRescheduleRequests",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRescheduleRequests_CompanyId",
                table: "AppointmentRescheduleRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServices_CompanyId",
                table: "AppointmentServices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServices_CompanyId_ProductId",
                table: "AppointmentServices",
                columns: new[] { "CompanyId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServices_ProductId",
                table: "AppointmentServices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSettings_CompanyId",
                table: "AppointmentSettings",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentStatusHistories_AppointmentId",
                table: "AppointmentStatusHistories",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentStatusHistories_CompanyId",
                table: "AppointmentStatusHistories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPageSettings_CompanyId",
                table: "BookingPageSettings",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingPageSettings_Slug",
                table: "BookingPageSettings",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppointmentProfessionals_ProfessionalId",
                table: "Appointments",
                column: "ProfessionalId",
                principalTable: "AppointmentProfessionals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppointmentServices_AppointmentServiceId",
                table: "Appointments",
                column: "AppointmentServiceId",
                principalTable: "AppointmentServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Customers_CustomerId",
                table: "Appointments",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Sales_SaleId",
                table: "Appointments",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'no_overlapping_active_appointments'
                    ) THEN
                        ALTER TABLE ""Appointments"" ADD CONSTRAINT ""no_overlapping_active_appointments""
                        EXCLUDE USING GIST (
                            ""CompanyId"" WITH =,
                            ""ProfessionalId"" WITH =,
                            tstzrange(""StartAt"", ""EndAt"", '[)') WITH &&
                        ) WHERE (""ProfessionalId"" IS NOT NULL AND ""Status"" IN (0, 1, 2));
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Appointments\" DROP CONSTRAINT IF EXISTS \"no_overlapping_active_appointments\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppointmentProfessionals_ProfessionalId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppointmentServices_AppointmentServiceId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Customers_CustomerId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Sales_SaleId",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "AppointmentAvailabilityExceptions");

            migrationBuilder.DropTable(
                name: "AppointmentEmployeeAvailabilities");

            migrationBuilder.DropTable(
                name: "AppointmentNotifications");

            migrationBuilder.DropTable(
                name: "AppointmentProfessionalServices");

            migrationBuilder.DropTable(
                name: "AppointmentRescheduleRequests");

            migrationBuilder.DropTable(
                name: "AppointmentSettings");

            migrationBuilder.DropTable(
                name: "AppointmentStatusHistories");

            migrationBuilder.DropTable(
                name: "BookingPageSettings");

            migrationBuilder.DropTable(
                name: "AppointmentProfessionals");

            migrationBuilder.DropTable(
                name: "AppointmentServices");

            migrationBuilder.DropIndex(
                name: "IX_Movements_CompanyId_MovementNumber",
                table: "Movements");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentServiceId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CompanyId_BookingCode",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CompanyId_ProfessionalId_StartAt_EndAt",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CustomerId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ManagementTokenHash",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ProfessionalId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_SaleId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "AppointmentServiceId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BillingStatus",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BookingCode",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ManagementTokenExpiresAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ManagementTokenHash",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ProfessionalId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "SaleId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Movements_CompanyId_MovementNumber",
                table: "Movements",
                columns: new[] { "CompanyId", "MovementNumber" },
                unique: true,
                filter: "\"MovementNumber\" IS NOT NULL AND \"MovementNumber\" <> '' AND \"IsDeleted\" = false");
        }
    }
}

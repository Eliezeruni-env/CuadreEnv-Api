using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Services.Concrete;
using Onion.Common.Exceptions;
using Onion.Common.Features;
using Onion.Common.Models;
using Onion.DataAccess;
using Onion.Domain;
using Onion.Domain.Appointments;
using Onion.Domain.Products;
using Xunit;
using AppointmentServiceEntity = Onion.Domain.Appointments.AppointmentService;

namespace Onion.BusinessLogic.Tests;

public sealed class AppointmentAgendaServiceTests
{
    private static OnionDbContext CreateInMemoryDbContext(string dbName, int companyId = 1)
    {
        var options = new DbContextOptionsBuilder<OnionDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var tenantMock = new Mock<ITenantProvider>();
        tenantMock.Setup(t => t.GetCompanyId()).Returns(companyId);

        var currentUserServiceMock = new Mock<Onion.Common.Services.ICurrentUserService>();
        currentUserServiceMock.Setup(u => u.IsGlobalAdministrator).Returns(true);
        currentUserServiceMock.Setup(u => u.CompanyId).Returns(companyId);

        return new OnionDbContext(options, tenantProvider: tenantMock.Object, currentUserService: currentUserServiceMock.Object);
    }

    [Fact]
    public void ComputeSha256_ProducesExpected64CharHex()
    {
        string raw = "my-secret-token-123";
        string hash = AppointmentAgendaService.ComputeSha256(raw);

        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, AppointmentAgendaService.ComputeSha256(raw)); // Deterministic
        Assert.NotEqual(hash, AppointmentAgendaService.ComputeSha256("different-token"));
    }

    [Fact]
    public async Task ExpireStalePendingAppointments_MarksOverdueAsExpired()
    {
        using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        int companyId = 1;

        var overdue = new Appointment
        {
            Id = 1,
            CompanyId = companyId,
            Status = AppointmentStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // expired 10 min ago
            StartAt = DateTime.UtcNow.AddHours(2),
            EndAt = DateTime.UtcNow.AddHours(2.5)
        };

        var validPending = new Appointment
        {
            Id = 2,
            CompanyId = companyId,
            Status = AppointmentStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(20), // still valid
            StartAt = DateTime.UtcNow.AddHours(3),
            EndAt = DateTime.UtcNow.AddHours(3.5)
        };

        await db.Appointments.AddRangeAsync(overdue, validPending);
        await db.SaveChangesAsync();

        var engine = new AvailabilityEngine(db);
        await engine.ExpireStalePendingAppointmentsAsync(companyId);

        var a1 = await db.Appointments.FindAsync(1);
        var a2 = await db.Appointments.FindAsync(2);

        Assert.NotNull(a1);
        Assert.Equal(AppointmentStatus.Expired, a1.Status);

        Assert.NotNull(a2);
        Assert.Equal(AppointmentStatus.Pending, a2.Status);
    }

    [Fact]
    public async Task AutoAssignProfessional_PicksProfessionalWithLeastAppointments()
    {
        using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        int companyId = 1;

        var prof1 = new AppointmentProfessional { Id = 1, CompanyId = companyId, DisplayName = "Carlos", IsActive = true, Active = true, DisplayOrder = 1 };
        var prof2 = new AppointmentProfessional { Id = 2, CompanyId = companyId, DisplayName = "Miguel", IsActive = true, Active = true, DisplayOrder = 2 };

        var service = new AppointmentServiceEntity { Id = 10, CompanyId = companyId, ProductId = 100, DurationMinutes = 30, Active = true };
        var ps1 = new AppointmentProfessionalService { ProfessionalId = 1, AppointmentServiceId = 10, CompanyId = companyId, IsActive = true };
        var ps2 = new AppointmentProfessionalService { ProfessionalId = 2, AppointmentServiceId = 10, CompanyId = companyId, IsActive = true };

        // Schedules for Monday
        var sched1 = new AppointmentEmployeeAvailability { ProfessionalId = 1, CompanyId = companyId, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(18, 0, 0), IsWorkingDay = true, Active = true };
        var sched2 = new AppointmentEmployeeAvailability { ProfessionalId = 2, CompanyId = companyId, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(18, 0, 0), IsWorkingDay = true, Active = true };

        // Prof 1 already has 2 appointments on Monday
        var monday = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc); // Monday
        var existingAppt1 = new Appointment { Id = 101, CompanyId = companyId, ProfessionalId = 1, Status = AppointmentStatus.Confirmed, StartAt = monday.AddHours(9), EndAt = monday.AddHours(9.5) };
        var existingAppt2 = new Appointment { Id = 102, CompanyId = companyId, ProfessionalId = 1, Status = AppointmentStatus.Confirmed, StartAt = monday.AddHours(10), EndAt = monday.AddHours(10.5) };

        await db.AppointmentProfessionals.AddRangeAsync(prof1, prof2);
        await db.AppointmentServices.AddAsync(service);
        await db.AppointmentProfessionalServices.AddRangeAsync(ps1, ps2);
        await db.AppointmentEmployeeAvailabilities.AddRangeAsync(sched1, sched2);
        await db.Appointments.AddRangeAsync(existingAppt1, existingAppt2);
        await db.SaveChangesAsync();

        var engine = new AvailabilityEngine(db);
        var targetSlot = monday.AddHours(14); // 2:00 PM
        var assigned = await engine.AutoAssignProfessionalAsync(companyId, 10, targetSlot, targetSlot.AddMinutes(30));

        Assert.NotNull(assigned);
        Assert.Equal(2, assigned.Id); // Miguel has 0 appointments, while Carlos has 2!
        Assert.Equal("Miguel", assigned.DisplayName);
    }

    [Fact]
    public async Task InvoiceAppointment_PreventsDoubleInvoicing()
    {
        using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        int companyId = 1;

        var product = new Product { Id = 50, CompanyId = companyId, Description = "Corte Clásico", Price = 500m };
        var service = new AppointmentServiceEntity { Id = 10, CompanyId = companyId, ProductId = 50, Product = product };
        var appt = new Appointment
        {
            Id = 1,
            CompanyId = companyId,
            Status = AppointmentStatus.Completed,
            BillingStatus = AppointmentBillingStatus.Unbilled,
            BookingCode = "CIT-000001",
            AppointmentService = service,
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddMinutes(30)
        };

        await db.Products.AddAsync(product);
        await db.AppointmentServices.AddAsync(service);
        await db.Appointments.AddAsync(appt);
        await db.SaveChangesAsync();

        var availabilityMock = new Mock<IAvailabilityEngine>();
        var featureMock = new Mock<IFeatureService>();

        var agendaService = new AppointmentAgendaService(db, availabilityMock.Object, featureMock.Object);

        // First invoicing should succeed
        int saleId = await agendaService.InvoiceAppointmentAsync(companyId, 1, cashSessionId: 10, customerId: 1, invoiceFolio: "FAC-001");
        Assert.True(saleId > 0);

        var updated = await db.Appointments.FindAsync(1);
        Assert.Equal(AppointmentBillingStatus.Billed, updated!.BillingStatus);
        Assert.Equal(saleId, updated.SaleId);

        // Second invoicing on same appointment must be rejected
        var ex = await Assert.ThrowsAsync<CustomException>(() =>
            agendaService.InvoiceAppointmentAsync(companyId, 1, cashSessionId: 10, customerId: 1, invoiceFolio: "FAC-002"));

        Assert.Equal("ALREADY_BILLED", ex.Error.Code);
    }

    [Fact]
    public async Task StateTransitions_EnforceBusinessRules()
    {
        using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        int companyId = 1;

        var appt = new Appointment
        {
            Id = 5,
            CompanyId = companyId,
            Status = AppointmentStatus.Cancelled,
            BookingCode = "CIT-000005",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddMinutes(30)
        };

        await db.Appointments.AddAsync(appt);
        await db.SaveChangesAsync();

        var agendaService = new AppointmentAgendaService(db, new Mock<IAvailabilityEngine>().Object, new Mock<IFeatureService>().Object);

        // Cannot complete or confirm an already cancelled appointment
        var ex = await Assert.ThrowsAsync<CustomException>(() =>
            agendaService.CompleteAppointmentAsync(companyId, 5));

        Assert.Equal("INVALID_STATE_TRANSITION", ex.Error.Code);
    }

    [Fact]
    public async Task AgendaAdminService_GetDashboard_ReturnsCorrectMetrics()
    {
        using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        int companyId = 1;

        var product = new Product { Id = 1, CompanyId = companyId, Description = "Barba", Price = 300m };
        var service = new AppointmentServiceEntity { Id = 1, CompanyId = companyId, ProductId = 1, Product = product, DurationMinutes = 20 };
        var prof = new AppointmentProfessional { Id = 1, CompanyId = companyId, DisplayName = "Andrés", IsActive = true };

        var today = DateTime.UtcNow.Date;
        var appt1 = new Appointment
        {
            Id = 1,
            CompanyId = companyId,
            Status = AppointmentStatus.Confirmed,
            AppointmentService = service,
            Professional = prof,
            CustomerName = "Juan Perez",
            StartAt = today.AddHours(10),
            EndAt = today.AddHours(10.5)
        };
        var appt2 = new Appointment
        {
            Id = 2,
            CompanyId = companyId,
            Status = AppointmentStatus.Pending,
            AppointmentService = service,
            Professional = prof,
            CustomerName = "Pedro Diaz",
            StartAt = today.AddHours(11),
            EndAt = today.AddHours(11.5)
        };

        await db.Products.AddAsync(product);
        await db.AppointmentServices.AddAsync(service);
        await db.AppointmentProfessionals.AddAsync(prof);
        await db.Appointments.AddRangeAsync(appt1, appt2);
        await db.SaveChangesAsync();

        var availabilityMock = new Mock<IAvailabilityEngine>();
        var featureMock = new Mock<IFeatureService>();
        featureMock.Setup(f => f.CompanyHasFeatureAsync(companyId, "appointments")).ReturnsAsync(true);
        var adminService = new AgendaAdminService(db, availabilityMock.Object, featureMock.Object);

        var dashboard = await adminService.GetDashboardAsync(companyId);

        Assert.NotNull(dashboard);
        Assert.Equal(2, dashboard.CitasHoy);
        Assert.Equal(1, dashboard.Pendientes);
        Assert.Equal(1, dashboard.Confirmadas);
        Assert.Equal(2, dashboard.CitasDeHoy.Count);
        Assert.Equal(1, dashboard.SolicitudesPendientes.Count);
        Assert.Equal(7, dashboard.OcupacionSemanal.Count);
    }

    [Fact]
    public async Task AgendaAdminService_UpsertProfessional_CreatesDefaultSchedule()
    {
        using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        int companyId = 1;

        var availabilityMock = new Mock<IAvailabilityEngine>();
        var featureMock = new Mock<IFeatureService>();
        featureMock.Setup(f => f.CompanyHasFeatureAsync(companyId, "appointments")).ReturnsAsync(true);
        var adminService = new AgendaAdminService(db, availabilityMock.Object, featureMock.Object);

        var dto = new UpsertProfessionalDto(
            Id: null,
            UserId: null,
            DisplayName: "Jefferson R.",
            SpecialtyTitle: "Barbero Profesional",
            IsOwner: false,
            IsActive: true,
            DisplayOrder: 1,
            AssignedServiceIds: null
        );

        var created = await adminService.UpsertProfessionalAsync(companyId, dto);

        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Jefferson R.", created.DisplayName);

        // Verify default Mon-Sat schedule (6 days) was automatically populated
        var scheduleCount = await db.AppointmentEmployeeAvailabilities.CountAsync(a => a.ProfessionalId == created.Id);
        Assert.Equal(6, scheduleCount);
    }

    [Fact]
    public async Task PublicBooking_CreatesReservationWithHashedToken()
    {
        using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        int companyId = 1;
        string slug = "barberia-central";

        var page = new BookingPageSettings
        {
            CompanyId = companyId,
            Slug = slug,
            BusinessName = "Barbería Central",
            IsPublicActive = true
        };

        var product = new Product { Id = 10, CompanyId = companyId, Description = "Corte Clásico", Price = 500m };
        var service = new AppointmentServiceEntity { Id = 1, CompanyId = companyId, ProductId = 10, Product = product, DurationMinutes = 30, IsPubliclyBookable = true, Active = true };
        var prof = new AppointmentProfessional { Id = 1, CompanyId = companyId, DisplayName = "Carlos Barbero", IsActive = true, Active = true };

        await db.BookingPageSettings.AddAsync(page);
        await db.Products.AddAsync(product);
        await db.AppointmentServices.AddAsync(service);
        await db.AppointmentProfessionals.AddAsync(prof);
        await db.SaveChangesAsync();

        var availabilityMock = new Mock<IAvailabilityEngine>();
        availabilityMock.Setup(a => a.IsProfessionalAvailableAsync(companyId, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>())).ReturnsAsync(true);

        var featureMock = new Mock<IFeatureService>();
        featureMock.Setup(f => f.CompanyHasFeatureAsync(companyId, "appointments")).ReturnsAsync(true);

        var agendaService = new AppointmentAgendaService(db, availabilityMock.Object, featureMock.Object);

        var dto = new CreatePublicReservationDto(
            AppointmentServiceId: 1,
            ProfessionalId: 1,
            StartAt: DateTime.UtcNow.AddHours(4),
            CustomerName: "Carlos Méndez",
            CustomerPhone: "8095550134",
            CustomerEmail: "carlos@example.com",
            Notes: "Corte tradicional"
        );

        var result = await agendaService.CreatePublicReservationAsync(slug, dto);

        Assert.NotNull(result);
        Assert.StartsWith("CIT-", result.BookingCode);
        Assert.NotNull(result.ManagementToken);
        Assert.Equal(32, result.ManagementToken.Length); // UUID without dashes

        var savedAppt = await db.Appointments.FindAsync(result.AppointmentId);
        Assert.NotNull(savedAppt);
        // The token in DB must NOT match raw token directly, but be its SHA-256 hash!
        Assert.NotEqual(result.ManagementToken, savedAppt.ManagementTokenHash);
        Assert.Equal(AppointmentAgendaService.ComputeSha256(result.ManagementToken), savedAppt.ManagementTokenHash);

        // Verification via public status endpoint
        var statusDto = await agendaService.GetPublicAppointmentStatusAsync(slug, result.BookingCode, result.ManagementToken);
        Assert.NotNull(statusDto);
        Assert.Equal("Carlos Méndez", statusDto.CustomerName);
        Assert.Equal("Corte Clásico", statusDto.ServiceName);
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.DataAccess;
using Onion.Domain;
using Onion.Domain.Finance;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class CashSessionService : ICashSessionService
{
    private readonly OnionDbContext _db;
    private readonly ILogger<CashSessionService> _logger;
    private readonly Onion.Common.Services.ISecurityAlertPublisher _alerts;

    public CashSessionService(OnionDbContext db, ILogger<CashSessionService> logger, Onion.Common.Services.ISecurityAlertPublisher alerts)
    {
        _db = db;
        _logger = logger;
        _alerts = alerts;
    }

    public async Task<CashSessionDto> OpenAsync(int companyId, OpenCashSessionRequest request, int cashierUserId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0 || cashierUserId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (request.CashierUserId != cashierUserId && request.CashierUserId > 0) cashierUserId = request.CashierUserId;
        if (request.OpeningAmount < 0) throw new CashSessionException("INVALID_OPENING_AMOUNT", "Opening amount cannot be negative.");

        var alreadyOpen = await _db.CashSessions.AnyAsync(x => x.CompanyId == companyId && x.CashierUserId == cashierUserId && x.Status == CashSessionStatus.Open, cancellationToken);
        if (alreadyOpen) throw new CashSessionException("CASH_SESSION_ALREADY_OPEN", "The cashier already has an open session.");

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var register = request.CashRegisterId.HasValue
            ? await _db.CashRegisters.SingleOrDefaultAsync(x => x.Id == request.CashRegisterId.Value && x.CompanyId == companyId, cancellationToken)
            : null;
        if (register is not null && register.Status != CashRegisterStatus.CLOSED)
            throw new CashSessionException("CASH_REGISTER_ALREADY_OPEN", "The cash register is already open.");
        if (register is null)
        {
            register = new CashRegister
            {
                CompanyId = companyId,
                OpeningAmount = decimal.Round(request.OpeningAmount, 2),
                InitialAmount = decimal.Round(request.OpeningAmount, 2),
                OpenedAt = DateTime.UtcNow,
                IsOpen = true,
                Status = CashRegisterStatus.OPEN,
                OpenedByUserId = cashierUserId
            };
            await _db.CashRegisters.AddAsync(register, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            register.OpeningAmount = decimal.Round(request.OpeningAmount, 2);
            register.InitialAmount = register.OpeningAmount;
            register.OpenedAt = DateTime.UtcNow;
            register.IsOpen = true;
            register.Status = CashRegisterStatus.OPEN;
            register.OpenedByUserId = cashierUserId;
        }

        var session = new CashSession
        {
            CompanyId = companyId,
            CashierUserId = cashierUserId,
            CashRegisterId = register.Id,
            OpeningAmount = decimal.Round(request.OpeningAmount, 2),
            OpeningDenominationsJson = Serialize(request.OpeningDenominations),
            CreateBy = cashierUserId.ToString()
        };
        await _db.CashSessions.AddAsync(session, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(session);
    }

    public async Task<CashSessionDto?> GetActiveAsync(int companyId, int cashierUserId, CancellationToken cancellationToken = default)
    {
        var session = await _db.CashSessions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CompanyId == companyId && x.CashierUserId == cashierUserId && x.Status == CashSessionStatus.Open, cancellationToken);
        return session is null ? null : ToDto(session);
    }

    public async Task<CashMovementDto> AddMovementAsync(int companyId, int sessionId, AddCashMovementRequest request, int userId, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0) throw new CashSessionException("INVALID_MOVEMENT_AMOUNT", "Movement amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new CashSessionException("MOVEMENT_REASON_REQUIRED", "Movement reason is required.");
        var session = await _db.CashSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.CompanyId == companyId, cancellationToken)
            ?? throw new CashSessionException("CASH_SESSION_NOT_FOUND", "Cash session was not found.");
        if (session.Status != CashSessionStatus.Open) throw new CashSessionException("CASH_SESSION_CLOSED", "A closed session cannot receive movements.");
        if (request.Type is not (CashMovementType.CashIn or CashMovementType.CashOut))
            throw new CashSessionException("INVALID_MOVEMENT_TYPE", "Only CashIn and CashOut are allowed for manual movements.");

        var expected = await CalculateExpectedCashAsync(session, cancellationToken);
        if (request.Type == CashMovementType.CashOut && request.Amount > expected)
            throw new CashSessionException("INSUFFICIENT_CASH", "The cash withdrawal is greater than the available cash.");

        var movement = new CashMovement
        {
            CompanyId = companyId,
            CashRegisterId = session.CashRegisterId,
            CashSessionId = session.Id,
            Amount = decimal.Round(request.Amount, 2),
            Reason = request.Reason.Trim(),
            Description = request.Reason.Trim(),
            ApprovedByUserId = request.AuthorizedBySupervisorId,
            Type = request.Type,
            CreateBy = userId.ToString()
        };
        await _db.CashMovements.AddAsync(movement, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        if (request.Type == CashMovementType.CashOut && movement.Amount >= 5000m)
            await _alerts.PublishAsync(Onion.Common.Services.SecurityAlertEvents.HighCashDrop, companyId, new { movementId = movement.Id, amount = movement.Amount, userId }, cancellationToken);
        return new CashMovementDto(movement.Id, session.Id, movement.Type, movement.Amount, movement.Reason, movement.RecordedAt);
    }

    public async Task<CashSessionDto> CloseAsync(int companyId, int sessionId, CloseCashSessionRequest request, int userId, CancellationToken cancellationToken = default)
    {
        if (request.DeclaredCash < 0 || request.DeclaredCards < 0 || request.DeclaredTransfers < 0)
            throw new CashSessionException("INVALID_DECLARED_AMOUNT", "Declared amounts cannot be negative.");
        var session = await _db.CashSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.CompanyId == companyId, cancellationToken)
            ?? throw new CashSessionException("CASH_SESSION_NOT_FOUND", "Cash session was not found.");
        if (session.Status != CashSessionStatus.Open) throw new CashSessionException("CASH_SESSION_CLOSED", "Cash session is already closed.");

        var expected = await CalculateExpectedCashAsync(session, cancellationToken);
        var difference = decimal.Round(request.DeclaredCash - expected, 2);
        var status = difference == 0 ? CashClosingStatus.Exact : difference < 0 ? CashClosingStatus.Shortage : CashClosingStatus.Surplus;
        var settings = await _db.CompanySettings.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        var tolerance = settings?.CashToleranceAmount ?? 50m;
        int? authorizedSupervisorId = null;
        if (Math.Abs(difference) > tolerance)
        {
            if (!request.SupervisorUserId.HasValue || request.SupervisorUserId <= 0 || string.IsNullOrWhiteSpace(request.SupervisorPin) || string.IsNullOrWhiteSpace(request.SupervisorNotes))
                throw new CashSessionException("OUT_OF_TOLERANCE_CLOSING", $"El descuadre de caja (RD$ {difference:0.00}) supera la tolerancia permitida (±RD$ {tolerance:0.00}). Se requiere PIN de autorización de supervisor.");

            var supervisor = await _db.Users.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == request.SupervisorUserId.Value && x.CompanyId == companyId && x.Active && !x.IsDeleted, cancellationToken);
            if (supervisor is null || supervisor.Role is not ("Admin" or "Manager" or "Supervisor") || !BCrypt.Net.BCrypt.Verify(request.SupervisorPin, supervisor.PasswordHash))
                throw new CashSessionException("INVALID_SUPERVISOR_PIN", "El PIN del supervisor no es válido o el usuario no tiene autorización.");
            authorizedSupervisorId = supervisor.Id;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        session.Close(expected, request.DeclaredCash, request.DeclaredCards, request.DeclaredTransfers, Serialize(request.Denominations), status, authorizedSupervisorId, request.SupervisorNotes);
        var register = await _db.CashRegisters.SingleAsync(x => x.Id == session.CashRegisterId && x.CompanyId == companyId, cancellationToken);
        register.Close(CashDiscrepancy.Calculate(expected, request.DeclaredCash), authorizedSupervisorId ?? userId, Serialize(request.Denominations), DateTime.UtcNow);
        if (Math.Abs(difference) > tolerance)
        {
            await _db.AuditLogs.AddAsync(new Onion.Domain.Audit.AuditLog
            {
                CompanyId = companyId,
                UserId = userId,
                Action = "CASH_SESSION_CLOSED_WITH_DIFFERENCE",
                Entity = nameof(CashSession),
                EntityId = session.Id,
                EntityName = session.Id.ToString(),
                PerformedBy = userId.ToString(),
                Details = request.SupervisorNotes,
                AuditPayload = JsonSerializer.Serialize(new { expected, declared = request.DeclaredCash, difference, tolerance })
            }, cancellationToken);
        }
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (Math.Abs(difference) > tolerance)
            await _alerts.PublishAsync(Onion.Common.Services.SecurityAlertEvents.OutOfToleranceClose, companyId, new { sessionId, difference, tolerance, supervisorId = authorizedSupervisorId }, cancellationToken);
        return ToDto(session);
    }

    private async Task<decimal> CalculateExpectedCashAsync(CashSession session, CancellationToken cancellationToken)
    {
        var movements = await _db.CashMovements.AsNoTracking().Where(x => x.CashSessionId == session.Id).ToListAsync(cancellationToken);
        var sales = await _db.Sales.AsNoTracking().Where(x => x.CashSessionId == session.Id && x.PaymentType == PaymentType.CASH && x.Status != SaleStatus.CANCELLED).SumAsync(x => (decimal?)x.PaidAmount, cancellationToken) ?? 0m;
        var refunds = movements.Where(x => x.Type == CashMovementType.Refund).Sum(x => x.Amount);
        var cashIn = movements.Where(x => x.Type is CashMovementType.CashIn or CashMovementType.ReceivablePayment).Sum(x => x.Amount);
        var cashOut = movements.Where(x => x.Type == CashMovementType.CashOut).Sum(x => x.Amount);
        return decimal.Round(session.OpeningAmount + sales + cashIn - cashOut - refunds, 2);
    }

    private static string Serialize(object? value) => value is null ? "{}" : JsonSerializer.Serialize(value);
    private static CashSessionDto ToDto(CashSession x) => new(x.Id, x.CashRegisterId, x.CashierUserId, x.OpeningAmount, x.Status, x.OpenedAt, x.ClosedAt, x.ExpectedCash, x.DeclaredCash, x.Difference, x.ClosingStatus);
}

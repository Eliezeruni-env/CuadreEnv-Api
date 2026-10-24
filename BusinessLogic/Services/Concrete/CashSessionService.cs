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
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        var alreadyOpen = await _db.CashSessions.AnyAsync(x => x.CompanyId == companyId && x.CashierUserId == cashierUserId && x.Status == CashSessionStatus.Open, cancellationToken);
        if (alreadyOpen) throw new CashSessionException("CASH_SESSION_ALREADY_OPEN", "The cashier already has an open session.");
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
        session.ApplyMovement(CashMovementType.CashIn, session.OpeningAmount);
        await _db.CashSessions.AddAsync(session, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(session);
    }

    public async Task<CashSessionDto> CloseByRegisterOrSessionAsync(int companyId, int id, CloseCashSessionRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var session = await _db.CashSessions
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Status == CashSessionStatus.Open &&
                        (x.CashRegisterId == id || x.Id == id))
            .OrderByDescending(x => x.CashRegisterId == id)
            .ThenByDescending(x => x.OpenedAt)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
            throw new CashSessionException("CASH_SESSION_NOT_FOUND", "No open cash session was found for the supplied register or session.");

        return await CloseAsync(companyId, session.Id, request, userId, cancellationToken);
    }

    public async Task<CashSessionDto?> GetActiveAsync(int companyId, int cashierUserId, CancellationToken cancellationToken = default)
    {
        var session = await _db.CashSessions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CompanyId == companyId && x.CashierUserId == cashierUserId && x.Status == CashSessionStatus.Open, cancellationToken);
        if (session is null) return null;

        var payments = await _db.Payments.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.CashSessionId == session.Id && x.SaleId.HasValue)
            .ToListAsync(cancellationToken);
        var movements = await _db.CashMovements.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.CashSessionId == session.Id && x.Type == CashMovementType.CashOut)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        return ToDto(session) with
        {
            TotalCashSales = payments.Where(x => x.PaymentMethod == PaymentMethod.CASH).Sum(x => x.Amount),
            TotalCardSales = payments.Where(x => x.PaymentMethod == PaymentMethod.CARD).Sum(x => x.Amount),
            TotalTransferSales = payments.Where(x => x.PaymentMethod == PaymentMethod.TRANSFER).Sum(x => x.Amount),
            TotalCashWithdrawals = movements
        };
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
        session.ApplyMovement(request.Type, movement.Amount);
        await _db.CashMovements.AddAsync(movement, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        if (request.Type == CashMovementType.CashOut && movement.Amount >= 5000m)
            await _alerts.PublishAsync(Onion.Common.Services.SecurityAlertEvents.HighCashDrop, companyId, new { movementId = movement.Id, amount = movement.Amount, userId }, cancellationToken);
        return new CashMovementDto(movement.Id, session.Id, movement.Type, movement.Amount, movement.Reason, movement.RecordedAt, session.CurrentBalance);
    }

    public async Task<CashSessionDto> CloseAsync(int companyId, int sessionId, CloseCashSessionRequest request, int userId, CancellationToken cancellationToken = default)
    {
        if (request.ActualAmount < 0 || request.DeclaredCards < 0 || request.DeclaredTransfers < 0 ||
            request.RetainedFloat is < 0m || request.CashToDeposit is < 0m || request.TransferVoucherCount < 0)
            throw new CashSessionException("INVALID_DECLARED_AMOUNT", "Declared amounts cannot be negative.");
        var session = await _db.CashSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.CompanyId == companyId, cancellationToken)
            ?? throw new CashSessionException("CASH_SESSION_NOT_FOUND", "Cash session was not found.");
        if (session.Status != CashSessionStatus.Open) throw new CashSessionException("CASH_SESSION_CLOSED", "Cash session is already closed.");

        var expected = await CalculateExpectedCashAsync(session, cancellationToken);
        var difference = decimal.Round(request.ActualAmount - expected, 2);

                if (Math.Abs(difference) > 0.01m)
        {
            if (string.IsNullOrWhiteSpace(request.Notes))
                request.Notes = $"Descuadre en arqueo de caja (Diferencia: RD$ {difference:N2})";
            else if (request.Notes.Trim().Length < 10)
                request.Notes = $"{request.Notes.Trim()} - Diferencia: RD$ {difference:N2}";
        }

        if (request.ActualAmount == 0m && expected > 0m && !ConfirmsEmptyDrawer(request.Notes))
        {
            request.Notes = (string.IsNullOrWhiteSpace(request.Notes) ? "" : request.Notes.Trim() + " ") + "[Gaveta vacia confirmada en arqueo]";
        }

        var retainedFloat = decimal.Round(request.RetainedFloat ?? 0m, 2);
        var cashToDeposit = decimal.Round(request.CashToDeposit ?? 0m, 2);
        if ((request.RetainedFloat.HasValue || request.CashToDeposit.HasValue) &&
            (retainedFloat > request.ActualAmount || cashToDeposit > request.ActualAmount ||
             (request.RetainedFloat.HasValue && request.CashToDeposit.HasValue &&
              decimal.Round(retainedFloat + cashToDeposit, 2) != decimal.Round(request.ActualAmount, 2))))
            throw new CashSessionException("INVALID_CASH_DESTINATION", "El fondo retenido y el efectivo a depositar deben ser coherentes con el efectivo contado.");

        var openSales = await _db.Sales.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.CashSessionId == session.Id &&
                        (x.Status == SaleStatus.PENDING || x.Status == SaleStatus.PARTIAL))
            .CountAsync(cancellationToken);
        if (openSales > 0)
                throw new CashSessionException("OPEN_CASH_OPERATIONS", $"No se puede cerrar la caja: existen {openSales} venta(s) en espera que deben completarse o anularse primero.");

        var status = difference == 0 ? CashClosingStatus.Exact : difference < 0 ? CashClosingStatus.Shortage : CashClosingStatus.Surplus;
        var settings = await _db.CompanySettings.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        var tolerance = settings?.CashToleranceAmount ?? 50m;
        int? authorizedSupervisorId = null;
        var requiresSupervisor = Math.Abs(difference) > tolerance ||
            (expected > 0m && request.ActualAmount == 0m && expected - request.ActualAmount > expected * 0.5m);
        if (requiresSupervisor)
        {
            var supervisor = await _db.Users.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Active && !x.IsDeleted && (x.Role == "Admin" || x.Role == "Manager" || x.Role == "Supervisor"), cancellationToken);
            authorizedSupervisorId = supervisor?.Id ?? userId;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        session.Close(expected, request.ActualAmount, request.DeclaredCards, request.DeclaredTransfers, request.BreakdownJson ?? Serialize(request.Denominations), status, authorizedSupervisorId, request.Notes, request.CardBatchNumber, request.TransferVoucherCount, retainedFloat, cashToDeposit, request.DepositDestination);
        var register = await _db.CashRegisters.SingleOrDefaultAsync(x => x.Id == session.CashRegisterId && x.CompanyId == companyId, cancellationToken);
        if (register != null)
        {
            register.Close(CashDiscrepancy.Calculate(expected, request.ActualAmount), authorizedSupervisorId ?? userId, request.BreakdownJson ?? Serialize(request.Denominations), DateTime.UtcNow);
        }

        if (cashToDeposit > 0m)
        {
            var destination = string.IsNullOrWhiteSpace(request.DepositDestination) ? "Bóveda" : request.DepositDestination.Trim();
            await _db.CashMovements.AddAsync(new CashMovement
            {
                CompanyId = companyId,
                CashRegisterId = session.CashRegisterId,
                CashSessionId = session.Id,
                Type = CashMovementType.CashOut,
                Amount = cashToDeposit,
                Reason = $"Retiro por cierre de turno a {destination}",
                Description = $"Retiro por cierre de turno a {destination} (Fondo retenido en gaveta: RD$ {retainedFloat:N2})",
                ApprovedByUserId = authorizedSupervisorId,
                CreateBy = userId.ToString(),
                RecordedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        if (requiresSupervisor)
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
                Details = request.Notes,
                AuditPayload = JsonSerializer.Serialize(new { expected, declared = request.ActualAmount, difference, tolerance, retainedFloat, cashToDeposit })
            }, cancellationToken);
        }
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (requiresSupervisor)
            await _alerts.PublishAsync(Onion.Common.Services.SecurityAlertEvents.OutOfToleranceClose, companyId, new { sessionId, difference, tolerance, supervisorId = authorizedSupervisorId }, cancellationToken);
        return ToDto(session);
    }

    private async Task<decimal> CalculateExpectedCashAsync(CashSession session, CancellationToken cancellationToken)
    {
        var movements = await _db.CashMovements.AsNoTracking().Where(x => x.CashSessionId == session.Id).ToListAsync(cancellationToken);
        var cashPayments = await _db.Payments.AsNoTracking()
            .Where(x => x.CashSessionId == session.Id && x.PaymentMethod == PaymentMethod.CASH && x.SaleId.HasValue)
            .Join(_db.Sales.AsNoTracking().Where(x => x.Status != SaleStatus.CANCELLED), p => p.SaleId, s => (int?)s.Id, (p, s) => p.Amount)
            .SumAsync(x => (decimal?)x, cancellationToken) ?? 0m;
        var legacyCashSales = await _db.Sales.AsNoTracking()
            .Where(x => x.CashSessionId == session.Id && x.PaymentType == PaymentType.CASH && x.Status != SaleStatus.CANCELLED &&
                        !_db.Payments.Any(p => p.SaleId == (int?)x.Id))
            .SumAsync(x => (decimal?)x.PaidAmount, cancellationToken) ?? 0m;
        var sales = cashPayments + legacyCashSales;
        var refunds = movements.Where(x => x.Type == CashMovementType.Refund).Sum(x => x.Amount);
        var cashIn = movements.Where(x => x.Type is CashMovementType.CashIn or CashMovementType.ReceivablePayment).Sum(x => x.Amount);
        var cashOut = movements.Where(x => x.Type == CashMovementType.CashOut).Sum(x => x.Amount);
        return decimal.Round(session.OpeningAmount + sales + cashIn - cashOut - refunds, 2);
    }

    private static string Serialize(object? value) => value is null ? "{}" : JsonSerializer.Serialize(value);
    private static bool ConfirmsEmptyDrawer(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return false;
        var normalized = notes.Trim().ToLowerInvariant();
        return normalized.Contains("gaveta vacía") || normalized.Contains("gaveta vacia") ||
               normalized.Contains("caja vacía") || normalized.Contains("caja vacia") ||
               normalized.Contains("drawer empty") || normalized.Contains("efectivo en cero");
    }

    private static CashSessionDto ToDto(CashSession x) => new(
        x.Id, x.CashRegisterId, x.CashierUserId, x.OpeningAmount, x.Status, x.OpenedAt, x.ClosedAt,
        x.ExpectedCash, x.DeclaredCash, x.Difference, x.ClosingStatus, ExpectedAmount: x.ExpectedAmount,
        ActualAmount: x.ActualAmount, Diff: x.Difference, Notes: x.ClosingNotes, DeclaredCards: x.DeclaredCards ?? 0,
        CardBatchNumber: x.CardBatchNumber, DeclaredTransfers: x.DeclaredTransfers ?? 0,
        TransferVoucherCount: x.TransferVoucherCount, RetainedFloat: x.RetainedFloat, CashToDeposit: x.CashToDeposit,
        DepositDestination: x.DepositDestination, SupervisorAuthorized: x.SupervisorAuthorized);
}

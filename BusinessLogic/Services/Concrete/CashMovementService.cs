using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;
using Onion.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CashMovementService : ICashMovementService
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;
        private readonly OnionDbContext _db;

        public CashMovementService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService, OnionDbContext db)
        {
            _uow = uow;
            _paginationService = paginationService;
            _db = db;
        }

        public async Task<IEnumerable<CashMovement>> GetAllAsync()
        {
            return await _uow.CashMovements.ListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<CashMovement>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pn = System.Math.Max(1, pageNumber);
            var ps = System.Math.Clamp(pageSize, 1, 100);
            return await _uow.CashMovements.GetPagedAsync(pn, ps);
        }

        public async Task<CashMovement> AddAsync(CashMovement movement)
        {
            if (movement == null) throw new ArgumentNullException(nameof(movement));
            if (string.IsNullOrWhiteSpace(movement.Reason))
                movement.Reason = !string.IsNullOrWhiteSpace(movement.Description) ? movement.Description : "Movimiento de caja";
            if (movement.Amount == 0m)
                throw new ArgumentException("Cash movement amount cannot be zero.", nameof(movement));
            movement.Reason = movement.Reason.Trim();
            movement.RecordedAt = DateTime.UtcNow;
            if (movement.CashSessionId.HasValue)
            {
                var session = await _db.CashSessions.SingleOrDefaultAsync(x => x.Id == movement.CashSessionId.Value && x.CashRegisterId == movement.CashRegisterId);
                if (session is null) throw new InvalidOperationException("Cash session was not found.");
                if (session.Status != CashSessionStatus.Open) throw new InvalidOperationException("A closed session cannot receive movements.");
                if (movement.Type is not (CashMovementType.CashIn or CashMovementType.CashOut))
                    throw new ArgumentException("Manual movements must be cash in or cash out.", nameof(movement.Type));
                if (movement.Type == CashMovementType.CashOut && movement.Amount > session.CurrentBalance)
                    throw new InvalidOperationException("The withdrawal exceeds the current cash balance.");
                session.ApplyMovement(movement.Type, movement.Amount);
            }
            await _uow.CashMovements.AddAsync(movement);
            await _uow.SaveChangesAsync();
            return movement;
        }
    }
}

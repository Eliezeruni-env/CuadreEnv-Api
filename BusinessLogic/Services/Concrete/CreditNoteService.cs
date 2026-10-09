using System;
using System.Linq;
using System.Threading.Tasks;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CreditNoteService : ICreditNoteService
    {
        private readonly IRepository<CreditNote> _creditNoteRepo;
        private readonly OnionDbContext _db;
        private readonly Onion.BussinesLogic.Services.Abstract.IWarehouseService _warehouseService;
        private readonly Onion.BussinesLogic.Services.Abstract.ICashMovementService _cashMovementService;
        private readonly Onion.BussinesLogic.Services.Abstract.ICashRegisterService _cashRegisterService;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;

        public CreditNoteService(
            IRepository<CreditNote> creditNoteRepo,
            OnionDbContext db,
            Onion.BussinesLogic.Services.Abstract.IWarehouseService warehouseService,
            Onion.BussinesLogic.Services.Abstract.ICashMovementService cashMovementService,
            Onion.BussinesLogic.Services.Abstract.ICashRegisterService cashRegisterService,
            Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _creditNoteRepo = creditNoteRepo;
            _db = db;
            _warehouseService = warehouseService;
            _cashMovementService = cashMovementService;
            _cashRegisterService = cashRegisterService;
            _currentUserService = currentUserService;
        }

        public async Task<CreditNoteDto> CreateAsync(int companyId, CreateCreditNoteRequest request)
        {
            // Use DB transaction to ensure atomicity of note, stock reintegration and cash movement
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Create credit note entity
                var note = new CreditNote
                {
                    CompanyId = companyId,
                    CustomerId = request.CustomerId,
                    Date = DateTime.UtcNow,
                    Reason = request.Reason
                };

                // generate number simple sequential (could be improved)
                note.Number = $"CN-{DateTime.UtcNow:yyyyMMddHHmmss}";

                foreach (var d in request.Details)
                {
                    var nd = new CreditNoteDetail
                    {
                        ProductId = d.ProductId,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice
                    };
                    note.Details.Add(nd);
                }

                note.TotalAmount = note.Details.Sum(x => x.LineTotal);

                await _creditNoteRepo.AddAsync(note);
                await _db.SaveChangesAsync();

                // Stock reintegration: add stock back to the first available warehouse for the company
                var warehouses = await _warehouseService.GetAllAsync(companyId);
                var warehouseForCompany = warehouses.FirstOrDefault();
                if (warehouseForCompany != null)
                {
                    var performedBy = _currentUserService?.UserId?.ToString() ?? "system";
                    foreach (var d in note.Details)
                    {
                        var req = new Onion.BussinesLogic.Dtos.MovementRequestDto(d.ProductId, warehouseForCompany.Id, d.Quantity);
                        await _warehouseService.AddStockAsync(req, performedBy);
                    }
                }

                // Cash egress: if amount > 0, register a cash movement (egress)
                if (note.TotalAmount > 0)
                {
                    var cashRegisters = await _cashRegisterService.GetAllAsync();
                    var openRegister = cashRegisters.FirstOrDefault(r => r.CompanyId == companyId && r.IsOpen);
                    if (openRegister != null)
                    {
                        var movement = new Onion.Domain.CashMovement
                        {
                            CashRegisterId = openRegister.Id,
                            Description = $"Credit note refund {note.Number}",
                            Amount = -note.TotalAmount,
                            CompanyId = companyId
                        };
                        await _cashMovementService.AddAsync(movement);
                    }
                }

                await tx.CommitAsync();

                var dto = new CreditNoteDto
                {
                    Id = note.Id,
                    Number = note.Number,
                    Date = note.Date,
                    CompanyId = note.CompanyId,
                    CustomerId = note.CustomerId,
                    TotalAmount = note.TotalAmount,
                    Reason = note.Reason,
                    Details = note.Details.Select(x => new CreditNoteDetailDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        LineTotal = x.LineTotal
                    }).ToList()
                };

                return dto;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}

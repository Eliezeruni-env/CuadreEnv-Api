using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Warehouses;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _uow;

        public WarehouseService(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, int companyId)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            var w = new Warehouse { Name = dto.Name?.Trim() ?? string.Empty, CompanyId = companyId };
            await _uow.Warehouses.AddAsync(w);
            await _uow.SaveChangesAsync();
            return new WarehouseDto(w.Id, w.Name, w.CompanyId);
        }

        public async Task<IEnumerable<WarehouseDto>> GetAllAsync()
        {
            var list = await _uow.Warehouses.ListAsync();
            return list.Select(w => new WarehouseDto(w.Id, w.Name, w.CompanyId));
        }

        public async Task AddStockAsync(MovementRequestDto req, string performedBy)
        {
            // Inbound
            using var tx = await _uow.BeginTransactionAsync();
            var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, req.WarehouseId);
            if (inv == null)
            {
                var wh = await _uow.Warehouses.GetByIdAsync(req.WarehouseId) ?? throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Warehouse not found", Language = "EN" });
                inv = new Inventory { ProductId = req.ProductId, WarehouseId = req.WarehouseId, Quantity = req.Quantity, CompanyId = wh.CompanyId };
                await _uow.Inventories.AddAsync(inv);
            }
            else
            {
                inv.Quantity += req.Quantity;
                _uow.Inventories.Update(inv);
            }

            var wh2 = await _uow.Warehouses.GetByIdAsync(req.WarehouseId);
            var mv = new Movement { ProductId = req.ProductId, ToWarehouseId = req.WarehouseId, Quantity = req.Quantity, Type = MovementType.Inbound, CompanyId = wh2?.CompanyId ?? 0, CreatedBy = performedBy };
            await _uow.Movements.AddAsync(mv);
            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task RemoveStockAsync(MovementRequestDto req, string performedBy)
        {
            using var tx = await _uow.BeginTransactionAsync();
            var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, req.WarehouseId);
            if (inv == null || inv.Quantity < req.Quantity)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = "Not enough stock", Language = "EN" });

            inv.Quantity -= req.Quantity;
            _uow.Inventories.Update(inv);

            var wh3 = await _uow.Warehouses.GetByIdAsync(req.WarehouseId);
            var mv = new Movement { ProductId = req.ProductId, FromWarehouseId = req.WarehouseId, Quantity = req.Quantity, Type = MovementType.Outbound, CompanyId = wh3?.CompanyId ?? 0, CreatedBy = performedBy };
            await _uow.Movements.AddAsync(mv);
            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task TransferStockAsync(TransferRequestDto req, string performedBy)
        {
            if (req.FromWarehouseId == req.ToWarehouseId)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INVALID_TRANSFER", Message = "Source and destination must differ", Language = "EN" });

            using var tx = await _uow.BeginTransactionAsync();

            var src = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, req.FromWarehouseId);
            if (src == null || src.Quantity < req.Quantity)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = "Not enough stock in source", Language = "EN" });

            var dest = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, req.ToWarehouseId);
            if (dest == null)
            {
                var whDest = await _uow.Warehouses.GetByIdAsync(req.ToWarehouseId) ?? throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Destination warehouse not found", Language = "EN" });
                dest = new Inventory { ProductId = req.ProductId, WarehouseId = req.ToWarehouseId, Quantity = req.Quantity, CompanyId = whDest.CompanyId };
                await _uow.Inventories.AddAsync(dest);
            }
            else
            {
                dest.Quantity += req.Quantity;
                _uow.Inventories.Update(dest);
            }

            src.Quantity -= req.Quantity;
            _uow.Inventories.Update(src);

            var whTransfer = await _uow.Warehouses.GetByIdAsync(req.FromWarehouseId) ?? await _uow.Warehouses.GetByIdAsync(req.ToWarehouseId);
            var mv = new Movement { ProductId = req.ProductId, FromWarehouseId = req.FromWarehouseId, ToWarehouseId = req.ToWarehouseId, Quantity = req.Quantity, Type = MovementType.Transfer, CompanyId = whTransfer?.CompanyId ?? 0, CreatedBy = performedBy };
            await _uow.Movements.AddAsync(mv);

            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }
}

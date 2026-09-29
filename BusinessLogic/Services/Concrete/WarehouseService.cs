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
        private readonly Onion.BussinesLogic.Services.Abstract.ISubscriptionService _subscriptionService;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;

        public WarehouseService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.ISubscriptionService subscriptionService, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _subscriptionService = subscriptionService;
            _paginationService = paginationService;
        }

        public async Task<IEnumerable<Onion.Domain.Inventory.InventoryMovement>> GetInventoryMovementsAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null)
        {
            var list = await _uow.InventoryMovements.ListAsync();
            var q = list.AsQueryable();
            if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(m => m.WarehouseId == warehouseId.Value);
            if (from.HasValue) q = q.Where(m => m.OccurredAt >= from.Value);
            if (to.HasValue) q = q.Where(m => m.OccurredAt <= to.Value);
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(m => m.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));
            return q.ToList();
        }

        public async Task<IEnumerable<ProductDto>> GetLowStockAsync()
        {
            var products = await _uow.Products.ListAsync();
            var low = products.Where(p => p.Stock < p.MinimumQuantity).Select(p => new ProductDto
            {
                Id = p.Id,
                Description = p.Description,
                Barcode = p.Barcode,
                CompanyId = p.CompanyId,
                Stock = p.Stock,
                MinimumQuantity = p.MinimumQuantity
            });
            return low;
        }

        public async Task<IEnumerable<MovementDto>> GetMovementHistoryAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null)
        {
            var list = await _uow.Movements.ListAsync();
            var q = list.AsQueryable();
            if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(m => (m.FromWarehouseId == warehouseId.Value) || (m.ToWarehouseId == warehouseId.Value));
            if (from.HasValue) q = q.Where(m => m.CreationDate >= from.Value);
            if (to.HasValue) q = q.Where(m => m.CreationDate <= to.Value);
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(m => m.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));
            return q.Select(m => new MovementDto(m.Id, m.ProductId, m.FromWarehouseId, m.ToWarehouseId, m.Quantity, m.Type.ToString()));
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<MovementDto>> GetMovementHistoryPagedAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null, int pageNumber = 1, int pageSize = 10)
        {
            var list = await _uow.Movements.ListAsync();
            var q = list.AsQueryable();
            if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(m => (m.FromWarehouseId == warehouseId.Value) || (m.ToWarehouseId == warehouseId.Value));
            if (from.HasValue) q = q.Where(m => m.CreationDate >= from.Value);
            if (to.HasValue) q = q.Where(m => m.CreationDate <= to.Value);
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(m => m.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));

            var dtoQuery = q.Select(m => new MovementDto(m.Id, m.ProductId, m.FromWarehouseId, m.ToWarehouseId, m.Quantity, m.Type.ToString()));
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            return await _paginationService.ToPagedListAsync(dtoQuery, pn, ps);
        }

        public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, int companyId)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            var canCreate = await _subscriptionService.CanCreateWarehouseAsync(companyId);
            if (!canCreate) throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "PLAN_LIMIT", Message = "Warehouse limit reached for current subscription plan", Language = "EN" });

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

        public async Task<Onion.Common.Models.Pagination.PagedList<WarehouseDto>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            var list = (await _uow.Warehouses.ListAsync()).Select(w => new WarehouseDto(w.Id, w.Name, w.CompanyId)).AsQueryable();
            return await _paginationService.ToPagedListAsync(list, pn, ps);
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<Onion.Domain.Inventory.InventoryMovement>> GetInventoryMovementsPagedAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null, int pageNumber = 1, int pageSize = 10)
        {
            var list = await _uow.InventoryMovements.ListAsync();
            var q = list.AsQueryable();
            if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(m => m.WarehouseId == warehouseId.Value);
            if (from.HasValue) q = q.Where(m => m.OccurredAt >= from.Value);
            if (to.HasValue) q = q.Where(m => m.OccurredAt <= to.Value);
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(m => m.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));

            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            return await _paginationService.ToPagedListAsync(q, pn, ps);
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

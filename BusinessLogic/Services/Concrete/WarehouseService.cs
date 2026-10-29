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
using Microsoft.EntityFrameworkCore;

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
            var low = products.Where(p => p.Active && !p.IsDeleted && !p.IsService && !p.InvoiceWithoutStock && p.ProductTypeId != 2 && p.MinimumQuantity > 0 && p.Stock <= p.MinimumQuantity).Select(p => new ProductDto
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
            var q = list.AsQueryable().Where(m => m.Active && !m.IsDeleted && m.Quantity > 0 && m.ProductId > 0);
            if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(m => (m.FromWarehouseId == warehouseId.Value) || (m.ToWarehouseId == warehouseId.Value));
            if (from.HasValue) q = q.Where(m => m.CreationDate >= from.Value);
            if (to.HasValue) q = q.Where(m => m.CreationDate <= to.Value);
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(m => m.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));

            var products = (await _uow.Products.ListAsync()).ToDictionary(p => p.Id);
            var warehouses = (await _uow.Warehouses.ListAsync()).ToDictionary(w => w.Id);
            return q.Select(m => ToMovementDto(m, products, warehouses)).ToList();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<MovementDto>> GetMovementHistoryPagedAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null, int pageNumber = 1, int pageSize = 10)
        {
            var list = await _uow.Movements.ListAsync();
            var q = list.AsQueryable().Where(m => m.Active && !m.IsDeleted && m.Quantity > 0 && m.ProductId > 0);
            if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(m => (m.FromWarehouseId == warehouseId.Value) || (m.ToWarehouseId == warehouseId.Value));
            if (from.HasValue) q = q.Where(m => m.CreationDate >= from.Value);
            if (to.HasValue) q = q.Where(m => m.CreationDate <= to.Value);
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(m => m.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));

            var products = (await _uow.Products.ListAsync()).ToDictionary(p => p.Id);
            var warehouses = (await _uow.Warehouses.ListAsync()).ToDictionary(w => w.Id);
            var dtoQuery = q.Select(m => ToMovementDto(m, products, warehouses)).AsQueryable();
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            return await _paginationService.ToPagedListAsync(dtoQuery, pn, ps);
        }

        private static MovementDto ToMovementDto(
            Movement movement,
            IReadOnlyDictionary<int, Onion.Domain.Products.Product> products,
            IReadOnlyDictionary<int, Warehouse> warehouses)
        {
            products.TryGetValue(movement.ProductId, out var product);
            var fromName = movement.FromWarehouseId.HasValue && warehouses.TryGetValue(movement.FromWarehouseId.Value, out var fromWarehouse)
                ? fromWarehouse.Name
                : null;
            var toName = movement.ToWarehouseId.HasValue && warehouses.TryGetValue(movement.ToWarehouseId.Value, out var toWarehouse)
                ? toWarehouse.Name
                : null;

            return new MovementDto(
                movement.Id,
                movement.ProductId,
                movement.FromWarehouseId,
                movement.ToWarehouseId,
                movement.Quantity,
                movement.Type.ToString(),
                product?.Description,
                product?.Barcode,
                product?.Reference,
                fromName,
                toName,
                movement.CreationDate);
        }

        public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, int companyId)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var w = new Warehouse { Name = dto.Name?.Trim() ?? string.Empty, CompanyId = companyId };
            await _uow.Warehouses.AddAsync(w);
            await _uow.SaveChangesAsync();
            return new WarehouseDto(w.Id, w.Name, w.CompanyId);
        }

        public async Task<IEnumerable<WarehouseDto>> GetAllAsync(int companyId)
        {
            return await _uow.Warehouses.Query()
                .Where(w => w.CompanyId == companyId)
                .Select(w => new WarehouseDto(w.Id, w.Name, w.CompanyId))
                .ToListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<WarehouseDto>> GetPagedAsync(int pageNumber, int pageSize, int companyId)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            var list = _uow.Warehouses.Query()
                .Where(w => w.CompanyId == companyId)
                .Select(w => new WarehouseDto(w.Id, w.Name, w.CompanyId));
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
            await EnsureNotServiceAsync(req.ProductId);

            decimal effectiveQuantity = req.Quantity;
            if (req.IsPackage == true && req.PackageFactor.HasValue && req.PackageFactor.Value > 0)
            {
                effectiveQuantity = req.Quantity * req.PackageFactor.Value;
            }

            using var tx = await _uow.BeginTransactionAsync();

            var wh = req.WarehouseId > 0 ? await _uow.Warehouses.GetByIdAsync(req.WarehouseId) : null;
            if (wh == null)
            {
                wh = (await _uow.Warehouses.ListAsync()).FirstOrDefault(w => w.Active && !w.IsDeleted);
            }
            int effectiveCompanyId = wh?.CompanyId > 0 ? wh.CompanyId : 1;
            int effectiveWarehouseId = wh?.Id > 0 ? wh.Id : (req.WarehouseId > 0 ? req.WarehouseId : 1);

            var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, effectiveWarehouseId);
            if (inv == null)
            {
                inv = new Inventory 
                { 
                    ProductId = req.ProductId, 
                    WarehouseId = effectiveWarehouseId, 
                    Quantity = effectiveQuantity, 
                    CompanyId = effectiveCompanyId,
                    CreateBy = performedBy,
                    ModifiedBy = performedBy
                };
                await _uow.Inventories.AddAsync(inv);
            }
            else
            {
                inv.Quantity += effectiveQuantity;
                inv.ModifiedBy = performedBy;
                _uow.Inventories.Update(inv);
            }

            // Sync product master stock
            var product = await _uow.Products.GetByIdAsync(req.ProductId);
            if (product != null)
            {
                product.Stock += effectiveQuantity;
                product.ModifiedBy = performedBy;
                _uow.Products.Update(product);
            }

            var mv = new Movement 
            { 
                ProductId = req.ProductId, 
                ToWarehouseId = effectiveWarehouseId, 
                Quantity = effectiveQuantity, 
                Type = MovementType.Inbound, 
                CompanyId = effectiveCompanyId, 
                CreatedBy = performedBy,
                CreateBy = performedBy,
                ModifiedBy = performedBy
            };
            await _uow.Movements.AddAsync(mv);
            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task RemoveStockAsync(MovementRequestDto req, string performedBy)
        {
            using var tx = await _uow.BeginTransactionAsync();

            var wh = req.WarehouseId > 0 ? await _uow.Warehouses.GetByIdAsync(req.WarehouseId) : null;
            if (wh == null)
            {
                wh = (await _uow.Warehouses.ListAsync()).FirstOrDefault(w => w.Active && !w.IsDeleted);
            }
            int effectiveCompanyId = wh?.CompanyId > 0 ? wh.CompanyId : 1;
            int effectiveWarehouseId = wh?.Id > 0 ? wh.Id : (req.WarehouseId > 0 ? req.WarehouseId : 1);

            var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, effectiveWarehouseId);
            if (inv != null)
            {
                inv.Quantity = Math.Max(0, inv.Quantity - req.Quantity);
                inv.ModifiedBy = performedBy;
                _uow.Inventories.Update(inv);
            }

            // Sync product master stock
            var product = await _uow.Products.GetByIdAsync(req.ProductId);
            if (product != null)
            {
                product.Stock = Math.Max(0, product.Stock - req.Quantity);
                product.ModifiedBy = performedBy;
                _uow.Products.Update(product);
            }

            var mv = new Movement 
            { 
                ProductId = req.ProductId, 
                FromWarehouseId = effectiveWarehouseId, 
                Quantity = req.Quantity, 
                Type = MovementType.Outbound, 
                CompanyId = effectiveCompanyId, 
                CreatedBy = performedBy,
                CreateBy = performedBy,
                ModifiedBy = performedBy
            };
            await _uow.Movements.AddAsync(mv);
            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task TransferStockAsync(TransferRequestDto req, string performedBy)
        {
            if (req.FromWarehouseId == req.ToWarehouseId)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INVALID_TRANSFER", Message = "Source and destination must differ", Language = "EN" });

            using var tx = await _uow.BeginTransactionAsync();

            var whDest = await _uow.Warehouses.GetByIdAsync(req.ToWarehouseId);
            var whSrc = await _uow.Warehouses.GetByIdAsync(req.FromWarehouseId);
            int effectiveCompanyId = whDest?.CompanyId > 0 ? whDest.CompanyId : (whSrc?.CompanyId > 0 ? whSrc.CompanyId : 1);

            var src = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, req.FromWarehouseId);
            if (src != null)
            {
                src.Quantity = Math.Max(0, src.Quantity - req.Quantity);
                src.ModifiedBy = performedBy;
                _uow.Inventories.Update(src);
            }

            var dest = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, req.ToWarehouseId);
            if (dest == null)
            {
                dest = new Inventory 
                { 
                    ProductId = req.ProductId, 
                    WarehouseId = req.ToWarehouseId, 
                    Quantity = req.Quantity, 
                    CompanyId = effectiveCompanyId,
                    CreateBy = performedBy,
                    ModifiedBy = performedBy
                };
                await _uow.Inventories.AddAsync(dest);
            }
            else
            {
                dest.Quantity += req.Quantity;
                dest.ModifiedBy = performedBy;
                _uow.Inventories.Update(dest);
            }

            var mv = new Movement 
            { 
                ProductId = req.ProductId, 
                FromWarehouseId = req.FromWarehouseId, 
                ToWarehouseId = req.ToWarehouseId, 
                Quantity = req.Quantity, 
                Type = MovementType.Transfer, 
                CompanyId = effectiveCompanyId, 
                CreatedBy = performedBy,
                CreateBy = performedBy,
                ModifiedBy = performedBy
            };
            await _uow.Movements.AddAsync(mv);

            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
        }

        private async Task EnsureNotServiceAsync(int productId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product != null)
            {
                var productType = product.ProductTypeId > 0 ? await _uow.ProductTypes.GetByIdAsync(product.ProductTypeId) : null;
                var isService = product.IsService
                    || product.ProductTypeId == 2
                    || string.Equals(productType?.Description?.Trim(), "Servicio", StringComparison.OrdinalIgnoreCase);

                if (isService)
                {
                    throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error
                    {
                        Code = "SERVICE_CANNOT_HAVE_STOCK",
                        Message = $"Los servicios no generan stock ni admiten movimientos de almacén ('{product.Description}').",
                        Language = "ES"
                    });
                }
            }
        }

}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess;
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
        private readonly ITenantProvider _tenantProvider;

        public WarehouseService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.ISubscriptionService subscriptionService, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService, ITenantProvider tenantProvider)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _subscriptionService = subscriptionService;
            _paginationService = paginationService;
            _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
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

        public async Task<IEnumerable<WarehouseStockDto>> GetWarehouseStockAsync(int warehouseId)
        {
            var warehouse = await GetWarehouseAsync(warehouseId);
            return (await _uow.Inventories.Query()
                    .Where(i => i.WarehouseId == warehouse.Id && i.CompanyId == warehouse.CompanyId && i.Quantity != 0)
                    .Select(i => new WarehouseStockDto(i.ProductId, i.Quantity))
                    .ToListAsync());
        }

        public async Task<IEnumerable<MovementDto>> GetMovementHistoryAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null)
        {
            var list = await _uow.Movements.ListAsync();
            var q = list.AsQueryable().Where(m => m.Active && !m.IsDeleted && m.Quantity > 0 && m.ProductId > 0);
            if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(m => (m.FromWarehouseId == warehouseId.Value) || (m.ToWarehouseId == warehouseId.Value));
            if (from.HasValue) q = q.Where(m => m.CreationDate >= from.Value);
            if (to.HasValue)
            {
                var toDate = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.Date.AddDays(1).AddTicks(-1) : to.Value;
                q = q.Where(m => m.CreationDate <= toDate);
            }
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(m => m.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));

            var products = (await _uow.Products.ListAsync()).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            var warehouses = (await _uow.Warehouses.ListAsync()).GroupBy(w => w.Id).ToDictionary(g => g.Key, g => g.First());
            return q.Select(m => ToMovementDto(m, products, warehouses)).ToList();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<MovementDto>> GetMovementHistoryPagedAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, string? type = null, int pageNumber = 1, int pageSize = 10)
        {
            var list = await _uow.Movements.ListAsync();
            var q = list.AsQueryable().Where(m => m.Active && !m.IsDeleted && m.Quantity > 0 && m.ProductId > 0);
            if (productId.HasValue) q = q.Where(m => m.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(m => (m.FromWarehouseId == warehouseId.Value) || (m.ToWarehouseId == warehouseId.Value));
            if (from.HasValue) q = q.Where(m => m.CreationDate >= from.Value);
            if (to.HasValue)
            {
                var toDate = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.Date.AddDays(1).AddTicks(-1) : to.Value;
                q = q.Where(m => m.CreationDate <= toDate);
            }
            if (!string.IsNullOrWhiteSpace(type)) q = q.Where(m => m.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));

            var products = (await _uow.Products.ListAsync()).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            var warehouses = (await _uow.Warehouses.ListAsync()).GroupBy(w => w.Id).ToDictionary(g => g.Key, g => g.First());
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

            var w = new Warehouse
            {
                Name = dto.Name?.Trim() ?? string.Empty,
                Code = dto.Code?.Trim() ?? string.Empty,
                CompanyId = companyId
            };
            await _uow.Warehouses.AddAsync(w);
            await _uow.SaveChangesAsync();
            if (string.IsNullOrWhiteSpace(w.Code))
            {
                w.Code = $"WH-{w.Id}";
                await _uow.SaveChangesAsync();
            }

            return new WarehouseDto(w.Id, w.Name, w.CompanyId, w.Code);
        }

        public async Task<IEnumerable<WarehouseDto>> GetAllAsync(int companyId)
        {
            return await _uow.Warehouses.Query()
                .Where(w => w.CompanyId == companyId)
                .Select(w => new WarehouseDto(w.Id, w.Name, w.CompanyId, w.Code))
                .ToListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<WarehouseDto>> GetPagedAsync(int pageNumber, int pageSize, int companyId)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            var list = _uow.Warehouses.Query()
                .Where(w => w.CompanyId == companyId)
                .Select(w => new WarehouseDto(w.Id, w.Name, w.CompanyId, w.Code));
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

        public async Task<MovementDto> AddStockAsync(MovementRequestDto req, string performedBy)
        {
            await EnsureMovementIsUniqueAsync(req.MovementNumber);
            var details = GetDetails(req.ProductDetails, req.Details);
            if (details.Count > 0) return await AddStockBatchAsync(req, details, performedBy);
            if (req.WarehouseId <= 0) throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "WAREHOUSE_REQUIRED", Message = "A valid warehouse is required.", Language = "EN" });
            if (req.Quantity <= 0) throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INVALID_QUANTITY", Message = "Quantity must be greater than zero.", Language = "EN" });
            await EnsureNotServiceAsync(req.ProductId);

            decimal effectiveQuantity = req.Quantity;
            if (req.IsPackage == true && req.PackageFactor.HasValue && req.PackageFactor.Value > 0)
            {
                effectiveQuantity = req.Quantity * req.PackageFactor.Value;
            }

            using var tx = await _uow.BeginTransactionAsync();

            var wh = await GetWarehouseAsync(req.WarehouseId);
            var effectiveCompanyId = wh.CompanyId;
            var effectiveWarehouseId = wh.Id;

            var inventoryUpdated = await _uow.Inventories.TryIncreaseStockAsync(
                req.ProductId, effectiveWarehouseId, effectiveCompanyId, effectiveQuantity, performedBy);
            if (!inventoryUpdated)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error
                {
                    Code = "PRODUCT_NOT_FOUND",
                    Message = "The product does not exist in the current company.",
                    Language = "EN"
                });

            // Sync product master stock
            if (!await _uow.Products.TryIncreaseStockAsync(req.ProductId, effectiveQuantity))
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error
                {
                    Code = "PRODUCT_NOT_FOUND",
                    Message = "The product does not exist in the current company.",
                    Language = "EN"
                });

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
            return new MovementDto(mv.Id, mv.ProductId, mv.FromWarehouseId, mv.ToWarehouseId, mv.Quantity, mv.Type.ToString(), CreationDate: mv.CreationDate);
        }

        public async Task<MovementDto> RemoveStockAsync(MovementRequestDto req, string performedBy)
        {
            await EnsureMovementIsUniqueAsync(req.MovementNumber);
            var details = GetDetails(req.ProductDetails, req.Details);
            if (details.Count > 0) return await RemoveStockBatchAsync(req, details, performedBy);
            if (req.WarehouseId <= 0) throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "WAREHOUSE_REQUIRED", Message = "A valid warehouse is required.", Language = "EN" });
            if (req.Quantity <= 0) throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INVALID_QUANTITY", Message = "Quantity must be greater than zero.", Language = "EN" });
            await EnsureNotServiceAsync(req.ProductId);
            using var tx = await _uow.BeginTransactionAsync();

            var wh = await GetWarehouseAsync(req.WarehouseId);
            var effectiveCompanyId = wh.CompanyId;
            var effectiveWarehouseId = wh.Id;

            var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(req.ProductId, effectiveWarehouseId);
            if (inv is null || inv.Quantity < req.Quantity)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = "Insufficient stock in the selected warehouse.", Language = "EN" });
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
            return new MovementDto(mv.Id, mv.ProductId, mv.FromWarehouseId, mv.ToWarehouseId, mv.Quantity, mv.Type.ToString(), CreationDate: mv.CreationDate);
        }

        public async Task<MovementDto> TransferStockAsync(TransferRequestDto req, string performedBy)
        {
            await EnsureMovementIsUniqueAsync(req.MovementNumber);
            var details = GetDetails(req.ProductDetails, req.Details);
            if (details.Count > 0) return await TransferStockBatchAsync(req, details, performedBy);
            if (req.FromWarehouseId <= 0 || req.ToWarehouseId <= 0)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "WAREHOUSE_REQUIRED", Message = "Valid source and destination warehouses are required.", Language = "EN" });
            if (req.Quantity <= 0)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INVALID_QUANTITY", Message = "Quantity must be greater than zero.", Language = "EN" });
            if (req.FromWarehouseId == req.ToWarehouseId)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INVALID_TRANSFER", Message = "Source and destination must differ", Language = "EN" });

            using var tx = await _uow.BeginTransactionAsync();

            var whDest = await _uow.Warehouses.GetByIdAsync(req.ToWarehouseId);
            var whSrc = await _uow.Warehouses.GetByIdAsync(req.FromWarehouseId);
            if (whDest is null || whSrc is null)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "WAREHOUSE_NOT_FOUND", Message = "Source or destination warehouse was not found.", Language = "EN" });
            if (whDest.CompanyId != whSrc.CompanyId)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "WAREHOUSE_COMPANY_MISMATCH", Message = "Source and destination warehouses must belong to the same company.", Language = "EN" });
            var effectiveCompanyId = whDest.CompanyId;

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
            return new MovementDto(mv.Id, mv.ProductId, mv.FromWarehouseId, mv.ToWarehouseId, mv.Quantity, mv.Type.ToString(), CreationDate: mv.CreationDate);
        }

        private static List<WarehouseMovementDetailDto> GetDetails(IEnumerable<WarehouseMovementDetailDto> primary, IEnumerable<WarehouseMovementDetailDto> secondary)
            => (primary?.Any() == true ? primary : secondary ?? Array.Empty<WarehouseMovementDetailDto>()).Where(d => d.ProductId > 0 && d.Quantity > 0).ToList();

        private async Task EnsureMovementIsUniqueAsync(string? movementNumber)
        {
            if (string.IsNullOrWhiteSpace(movementNumber)) return;
            var exists = await _uow.Movements.Query().AnyAsync(m => m.MovementNumber == movementNumber);
            if (exists)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error
                {
                    Code = "DUPLICATE_MOVEMENT",
                    Message = "A movement with this idempotency key already exists.",
                    Language = "EN"
                });
        }

        private async Task<MovementDto> AddStockBatchAsync(MovementRequestDto req, List<WarehouseMovementDetailDto> details, string performedBy)
        {
            var wh = await GetWarehouseAsync(req.WarehouseId);
            await using var tx = await _uow.BeginTransactionAsync();
            var movement = new Movement { ProductId = details[0].ProductId, ToWarehouseId = wh.Id, Type = MovementType.Inbound, CompanyId = wh.CompanyId, CreatedBy = performedBy, CreateBy = performedBy, ModifiedBy = performedBy, MovementNumber = req.MovementNumber };
            foreach (var detail in details)
            {
                await EnsureNotServiceAsync(detail.ProductId);
                if (!await _uow.Inventories.TryIncreaseStockAsync(detail.ProductId, wh.Id, wh.CompanyId, detail.Quantity, performedBy) || !await _uow.Products.TryIncreaseStockAsync(detail.ProductId, detail.Quantity))
                    throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "PRODUCT_NOT_FOUND", Message = "The product does not exist in the current company.", Language = "EN" });
                movement.Quantity += detail.Quantity;
                movement.TotalCost += detail.Cost > 0 ? detail.Cost : detail.Quantity * detail.UnitCost;
                movement.Details.Add(new MovementDetail { ProductId = detail.ProductId, ProductName = detail.ProductName, BarCode = detail.BarCode, Quantity = detail.Quantity, UnitCost = detail.UnitCost, TotalCost = detail.Cost > 0 ? detail.Cost : detail.Quantity * detail.UnitCost });
            }
            movement.UnitCost = movement.Quantity == 0 ? 0 : movement.TotalCost / movement.Quantity;
            await _uow.Movements.AddAsync(movement);
            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
            return new MovementDto(movement.Id, movement.ProductId, null, movement.ToWarehouseId, movement.Quantity, movement.Type.ToString(), CreationDate: movement.CreationDate, UnitCost: movement.UnitCost, TotalCost: movement.TotalCost, MovementNumber: movement.MovementNumber);
        }

        private async Task<MovementDto> RemoveStockBatchAsync(MovementRequestDto req, List<WarehouseMovementDetailDto> details, string performedBy)
        {
            var wh = await GetWarehouseAsync(req.WarehouseId);
            await using var tx = await _uow.BeginTransactionAsync();
            var movement = new Movement { ProductId = details[0].ProductId, FromWarehouseId = wh.Id, Type = MovementType.Outbound, CompanyId = wh.CompanyId, CreatedBy = performedBy, CreateBy = performedBy, ModifiedBy = performedBy, MovementNumber = req.MovementNumber };
            foreach (var detail in details)
            {
                await EnsureNotServiceAsync(detail.ProductId);
                var inventory = await _uow.Inventories.GetByProductAndWarehouseAsync(detail.ProductId, wh.Id);
                if (inventory is null || inventory.Quantity < detail.Quantity)
                    throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = "Insufficient stock in the selected warehouse.", Language = "EN" });
                if (inventory != null) { inventory.Quantity = Math.Max(0, inventory.Quantity - detail.Quantity); inventory.ModifiedBy = performedBy; _uow.Inventories.Update(inventory); }
                var product = await _uow.Products.GetByIdAsync(detail.ProductId);
                if (product != null) { product.Stock = Math.Max(0, product.Stock - detail.Quantity); product.ModifiedBy = performedBy; _uow.Products.Update(product); }
                movement.Quantity += detail.Quantity;
                movement.TotalCost += detail.Cost > 0 ? detail.Cost : detail.Quantity * detail.UnitCost;
                movement.Details.Add(new MovementDetail { ProductId = detail.ProductId, ProductName = detail.ProductName, BarCode = detail.BarCode, Quantity = detail.Quantity, UnitCost = detail.UnitCost, TotalCost = detail.Cost > 0 ? detail.Cost : detail.Quantity * detail.UnitCost });
            }
            movement.UnitCost = movement.Quantity == 0 ? 0 : movement.TotalCost / movement.Quantity;
            await _uow.Movements.AddAsync(movement); await _uow.SaveChangesAsync(); await tx.CommitAsync();
            return new MovementDto(movement.Id, movement.ProductId, movement.FromWarehouseId, null, movement.Quantity, movement.Type.ToString(), CreationDate: movement.CreationDate, UnitCost: movement.UnitCost, TotalCost: movement.TotalCost, MovementNumber: movement.MovementNumber);
        }

        private async Task<MovementDto> TransferStockBatchAsync(TransferRequestDto req, List<WarehouseMovementDetailDto> details, string performedBy)
        {
            if (req.FromWarehouseId == req.ToWarehouseId) throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INVALID_TRANSFER", Message = "Source and destination must differ", Language = "EN" });
            var source = await GetWarehouseAsync(req.FromWarehouseId); var destination = await GetWarehouseAsync(req.ToWarehouseId);
            if (source.CompanyId != destination.CompanyId) throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "WAREHOUSE_COMPANY_MISMATCH", Message = "Source and destination warehouses must belong to the same company.", Language = "EN" });
            await using var tx = await _uow.BeginTransactionAsync();
            var movement = new Movement { ProductId = details[0].ProductId, FromWarehouseId = source.Id, ToWarehouseId = destination.Id, Type = MovementType.Transfer, CompanyId = source.CompanyId, CreatedBy = performedBy, CreateBy = performedBy, ModifiedBy = performedBy, MovementNumber = req.MovementNumber };
            foreach (var detail in details)
            {
                await EnsureNotServiceAsync(detail.ProductId);
                var origin = await _uow.Inventories.GetByProductAndWarehouseAsync(detail.ProductId, source.Id);
                if (origin is null || origin.Quantity < detail.Quantity)
                    throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = "Insufficient stock in the source warehouse.", Language = "EN" });
                origin.Quantity -= detail.Quantity; _uow.Inventories.Update(origin);
                var target = await _uow.Inventories.GetByProductAndWarehouseAsync(detail.ProductId, destination.Id) ?? new Inventory { ProductId = detail.ProductId, WarehouseId = destination.Id, CompanyId = source.CompanyId, CreateBy = performedBy, ModifiedBy = performedBy };
                target.Quantity += detail.Quantity; if (target.Id == 0) await _uow.Inventories.AddAsync(target); else _uow.Inventories.Update(target);
                movement.Quantity += detail.Quantity; movement.TotalCost += detail.Cost > 0 ? detail.Cost : detail.Quantity * detail.UnitCost;
                movement.Details.Add(new MovementDetail { ProductId = detail.ProductId, ProductName = detail.ProductName, BarCode = detail.BarCode, Quantity = detail.Quantity, UnitCost = detail.UnitCost, TotalCost = detail.Cost > 0 ? detail.Cost : detail.Quantity * detail.UnitCost });
            }
            movement.UnitCost = movement.Quantity == 0 ? 0 : movement.TotalCost / movement.Quantity;
            await _uow.Movements.AddAsync(movement); await _uow.SaveChangesAsync(); await tx.CommitAsync();
            return new MovementDto(movement.Id, movement.ProductId, source.Id, destination.Id, movement.Quantity, movement.Type.ToString(), CreationDate: movement.CreationDate, UnitCost: movement.UnitCost, TotalCost: movement.TotalCost, MovementNumber: movement.MovementNumber);
        }

        private async Task EnsureNotServiceAsync(int productId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product is null)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "PRODUCT_NOT_FOUND", Message = "Product was not found.", Language = "EN" });
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

        private async Task<Warehouse> GetWarehouseAsync(int warehouseId)
        {
            var warehouse = await _uow.Warehouses.GetByIdAsync(warehouseId);
            if (warehouse is null || !warehouse.Active || warehouse.IsDeleted)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "WAREHOUSE_NOT_FOUND", Message = "Warehouse was not found.", Language = "EN" });

            var tenantCompanyId = _tenantProvider.GetCompanyId();
            if (tenantCompanyId.HasValue && tenantCompanyId.Value > 0 && warehouse.CompanyId != tenantCompanyId.Value)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Warehouse does not belong to the current company.", Language = "EN" });

            return warehouse;
        }

}
}

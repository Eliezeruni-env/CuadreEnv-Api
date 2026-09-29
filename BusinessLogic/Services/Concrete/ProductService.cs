using AutoMapper;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.Common.Models.Pagination;
using Onion.DataAccess.Models;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Products;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;
        private readonly Onion.BussinesLogic.Services.Abstract.ISubscriptionService _subscriptionService;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;

        public ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService> logger, Onion.BussinesLogic.Services.Abstract.ISubscriptionService subscriptionService, Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger;
            _subscriptionService = subscriptionService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<IEnumerable<Product>> GetByTypeAsync(int productTypeId)
        {
            if (productTypeId <= 0) return Array.Empty<Product>();
            return (await _uow.Products.FindAsync(p => p.ProductTypeId == productTypeId && p.Active && !p.IsDeleted)).ToList();
        }

        // Domain-level operations
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _uow.Products.ListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _uow.Products.GetByIdAsync(id);
        }

        public async Task CreateAsync(Product product)
        {
            if (product is null) throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(product.Description))
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_NAME", Message = "Product name is required", Language = "EN" });
            if (await _uow.Products.ExistsByNameAsync(product.Description))
                throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_NAME", Message = "Product name already exists", Language = "EN" });

            // Validate barcode uniqueness if provided
            if (!string.IsNullOrWhiteSpace(product.Barcode))
            {
                var existsBarcode = await _uow.Products.ExistsByBarcodeAsync(product.Barcode.Trim(), 0);
                if (existsBarcode)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_BARCODE", Message = "Product barcode already exists", Language = "EN" });
            }

            // Validate related entities exist to prevent FK errors
            var pt = await _uow.ProductTypes.GetByIdAsync(product.ProductTypeId);
            if (pt == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "ProductType not found", Language = "EN" });

            var cat = await _uow.Categories.GetByIdAsync(product.CategoryId);
            if (cat == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Category not found", Language = "EN" });

            var comp = await _uow.Companies.GetByIdAsync(product.CompanyId);
            if (comp == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Company not found", Language = "EN" });

            if (product.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "EN" });

            if (product.Cost <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_COST", Message = "Cost must be greater than zero", Language = "EN" });

            // Enforce plan limits using injected subscription service
            if (!await _subscriptionService.CanCreateProductAsync(product.CompanyId))
                throw new CustomException(new Onion.Common.Models.Error { Code = "PLAN_LIMIT", Message = "Product limit reached for current subscription plan", Language = "EN" });

            await _uow.Products.AddAsync(product);
            try
            {
                await _uow.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Failed to create product {Description}", product.Description);
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new CustomException(new Onion.Common.Models.Error { Code = "DB_ERROR", Message = inner, Language = "EN" });
            }
        }

        public async Task UpdateAsync(Product product)
        {
            if (product is null) throw new ArgumentNullException(nameof(product));

            var existing = await _uow.Products.GetByIdAsync(product.Id)
                ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Product not found", Language = "EN" });

            if (!string.Equals(existing.Description, product.Description, StringComparison.OrdinalIgnoreCase))
            {
                if (await _uow.Products.ExistsByNameAsync(product.Description))
                    throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_NAME", Message = "Product name already exists", Language = "EN" });
            }

            // Validate barcode uniqueness when updating (exclude current product id)
            if (!string.IsNullOrWhiteSpace(product.Barcode))
            {
                var existsBarcode = await _uow.Products.ExistsByBarcodeAsync(product.Barcode.Trim(), product.Id);
                if (existsBarcode)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_BARCODE", Message = "Product barcode already exists", Language = "EN" });
            }

            // Validate related entities exist to prevent FK errors on update
            var pt2 = await _uow.ProductTypes.GetByIdAsync(product.ProductTypeId);
            if (pt2 == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "ProductType not found", Language = "EN" });

            var cat2 = await _uow.Categories.GetByIdAsync(product.CategoryId);
            if (cat2 == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Category not found", Language = "EN" });

            var comp2 = await _uow.Companies.GetByIdAsync(product.CompanyId);
            if (comp2 == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Company not found", Language = "EN" });

            if (product.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "EN" });

            if (product.Cost <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_COST", Message = "Cost must be greater than zero", Language = "EN" });

            // Apply allowed updates
            existing.Description = product.Description;
            existing.Barcode = product.Barcode;
            existing.ShortDescription = product.ShortDescription;
            existing.Reference = product.Reference;
            existing.MaximumQuantity = product.MaximumQuantity;
            existing.MinimumQuantity = product.MinimumQuantity;
            existing.ProductTypeId = product.ProductTypeId;
            existing.CategoryId = product.CategoryId;
            existing.UnitOfMeasurementId = product.UnitOfMeasurementId;
            existing.ExpirationDate = product.ExpirationDate;
            existing.InvoiceWithoutStock = product.InvoiceWithoutStock;
            existing.Cost = product.Cost;

            _uow.Products.Update(existing);
            try
            {
                await _uow.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Failed to update product {Id}", product.Id);
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new CustomException(new Onion.Common.Models.Error { Code = "DB_ERROR", Message = inner, Language = "EN" });
            }
        }

        public async Task DeleteAsyncDomain(int id)
        {
            var existing = await _uow.Products.GetByIdAsync(id)
                ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Product not found", Language = "EN" });

            _uow.Products.Remove(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<Product>> SearchAsync(string name)
        {
            return await _uow.Products.SearchByNameAsync(name);
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        {
            return await _uow.Products.GetByCategoryAsync(categoryId);
        }

        public async Task<IEnumerable<Product>> GetLowStockAsync(int threshold)
        {
            return await _uow.Products.GetLowStockAsync(threshold);
        }

        // DTO-based methods (backwards compatibility for controllers)
        public async Task<ProductDto?> GetAsync(int id)
        {
            var entity = await _uow.Products.GetByIdSqlAsync(id);
            return entity is null ? null : _mapper.Map<ProductDto>(entity);
        }

        public async Task<IEnumerable<ProductDto>> SearchByDescriptionAsync(string description)
        {
            var entities = await _uow.Products.SearchByDescriptionAsync(description);
            return _mapper.Map<IEnumerable<ProductDto>>(entities);
        }

        public Task<PagedList<ProductRow>> GetPagedListAsync(FilterPayload filterPayload, CancellationToken ct = default)
        {
            return _uow.Products.GetPagedProductsAsync(filterPayload, ct);
        }

        public async Task AddAsync(ProductDto entityDto)
        {
            if (entityDto is null) throw new ArgumentNullException(nameof(entityDto));

            bool existsBarcode = await _uow.Products.ExistsByBarcodeAsync(entityDto.Barcode ?? string.Empty, 0);
            if (existsBarcode)
                throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_BARCODE", Message = "El código de barras ya existe", Language = "ES" });

            if (string.IsNullOrWhiteSpace(entityDto.Description))
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_NAME", Message = "Product name is required", Language = "ES" });

            if (await _uow.Products.ExistsByNameAsync(entityDto.Description))
                throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_NAME", Message = "Product name already exists", Language = "ES" });

            var entity = _mapper.Map<Product>(entityDto);

            // Ensure CompanyId is set from the current authenticated user's tenant context
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue || companyId.Value <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "COMPANY_REQUIRED", Message = "Company context is required to create products.", Language = "ES" });

            entity.CompanyId = companyId.Value;

            if (entity.ProductTypeId == 2)
                entity.InvoiceWithoutStock = true;

            if (entity.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "ES" });

            if (entity.Cost <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_COST", Message = "Cost must be greater than zero", Language = "ES" });

            // Validate related entities to prevent FK violations (only if provided)
            if (entity.ProductTypeId > 0)
            {
                var pt = await _uow.ProductTypes.GetByIdAsync(entity.ProductTypeId);
                if (pt == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "ProductType not found", Language = "ES" });
            }

            if (entity.CategoryId > 0)
            {
                var cat = await _uow.Categories.GetByIdAsync(entity.CategoryId);
                if (cat == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Category not found", Language = "ES" });
            }

            var comp = await _uow.Companies.GetByIdAsync(entity.CompanyId);
            if (comp == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Company not found", Language = "ES" });

            await _uow.Products.AddAsync(entity);
            try
            {
                await _uow.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Failed to add product DTO {Description}", entity.Description);
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new CustomException(new Onion.Common.Models.Error { Code = "DB_ERROR", Message = inner, Language = "ES" });
            }
        }

        public async Task UpdateAsync(ProductDto entityDto)
        {
            if (entityDto is null) throw new ArgumentNullException(nameof(entityDto));

            bool existsBarcode = await _uow.Products.ExistsByBarcodeAsync(entityDto.Barcode ?? string.Empty, entityDto.Id);
            if (existsBarcode)
                throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_BARCODE", Message = "El código de barras ya existe", Language = "ES" });

            var existing = await _uow.Products.GetByIdSqlAsync(entityDto.Id)
                ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "El producto no existe.", Language = "ES" });

            if (!string.Equals(existing.Description, entityDto.Description, StringComparison.OrdinalIgnoreCase))
            {
                if (await _uow.Products.ExistsByNameAsync(entityDto.Description))
                    throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_NAME", Message = "Product name already exists", Language = "ES" });
            }

            _mapper.Map(entityDto, existing);

            if (existing.ProductTypeId == 2)
                existing.InvoiceWithoutStock = true;

            if (existing.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "ES" });

            if (existing.Cost <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_COST", Message = "Cost must be greater than zero", Language = "ES" });

            // Validate related entities to avoid FK errors (only if product type provided)
            if (existing.ProductTypeId > 0)
            {
                var pt2 = await _uow.ProductTypes.GetByIdAsync(existing.ProductTypeId);
                if (pt2 == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "ProductType not found", Language = "ES" });
            }

            if (existing.CategoryId > 0)
            {
                var cat2 = await _uow.Categories.GetByIdAsync(existing.CategoryId);
                if (cat2 == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Category not found", Language = "ES" });
            }

            var comp2 = await _uow.Companies.GetByIdAsync(existing.CompanyId);
            if (comp2 == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Company not found", Language = "ES" });

            try
            {
                await _uow.Products.UpdateAsync(existing);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Failed to update product DTO {Id}", existing.Id);
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new CustomException(new Onion.Common.Models.Error { Code = "DB_ERROR", Message = inner, Language = "ES" });
            }
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Products.GetByIdSqlAsync(id)
                ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "El producto no existe.", Language = "ES" });

            await _uow.Products.DeleteAsync(id);
        }
    }
}

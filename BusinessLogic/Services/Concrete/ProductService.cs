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

        public ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService> logger)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger;
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

            if (product.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "EN" });

            if (product.Cost <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_COST", Message = "Cost must be greater than zero", Language = "EN" });

            await _uow.Products.AddAsync(product);
            await _uow.SaveChangesAsync();
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
            await _uow.SaveChangesAsync();
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

            if (entity.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "ES" });

            if (entity.Cost <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_COST", Message = "Cost must be greater than zero", Language = "ES" });

            await _uow.Products.AddAsync(entity);
            await _uow.SaveChangesAsync();
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

            if (existing.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "ES" });

            if (existing.Cost <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_COST", Message = "Cost must be greater than zero", Language = "ES" });

            await _uow.Products.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Products.GetByIdSqlAsync(id)
                ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "El producto no existe.", Language = "ES" });

            await _uow.Products.DeleteAsync(id);
        }
    }
}

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
using System.Net.Http;
using System.Text.Json;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;
        private readonly Onion.BussinesLogic.Services.Abstract.ISubscriptionService _subscriptionService;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService> logger, Onion.BussinesLogic.Services.Abstract.ISubscriptionService subscriptionService, Onion.Common.Services.ICurrentUserService currentUserService, IHttpClientFactory httpClientFactory)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger;
            _subscriptionService = subscriptionService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<ProductLookupDto?> LookupByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        {
            var normalizedBarcode = barcode?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedBarcode))
                return null;

            var local = await _uow.Products.GetByBarcodeAsync(normalizedBarcode);
            if (local is not null)
            {
                var category = local.CategoryId > 0
                    ? await _uow.Categories.GetByIdAsync(local.CategoryId)
                    : null;
                return new ProductLookupDto
                {
                    Barcode = normalizedBarcode,
                    Name = local.Description,
                    Brand = local.Brand,
                    Category = category?.Description,
                    Description = local.Description,
                    ImageUrl = local.ImageUrl,
                    IsLocal = true
                };
            }

            var client = _httpClientFactory.CreateClient("ProductLookup");

            // 1. Open Food Facts (v0 / v2 - enorme base comunitaria global de alimentos y bebidas)
            var off = await GetExternalProductAsync(
                client,
                $"https://world.openfoodfacts.org/api/v0/product/{Uri.EscapeDataString(normalizedBarcode)}.json",
                normalizedBarcode,
                cancellationToken);
            if (off is not null)
                return off;

            // 2. UPCitemdb (productos generales de consumo, electrónica, hogar, etc.)
            var upc = await GetExternalProductAsync(
                client,
                $"https://api.upcitemdb.com/prod/trial/lookup?upc={Uri.EscapeDataString(normalizedBarcode)}",
                normalizedBarcode,
                cancellationToken);
            if (upc is not null)
                return upc;

            // 3. Open Beauty Facts (cosmética, cuidado personal, higiene)
            var beauty = await GetExternalProductAsync(
                client,
                $"https://world.openbeautyfacts.org/api/v0/product/{Uri.EscapeDataString(normalizedBarcode)}.json",
                normalizedBarcode,
                cancellationToken);
            if (beauty is not null)
                return beauty;

            // 4. Open Products Facts (ferretería, limpieza, artículos varios)
            var prod = await GetExternalProductAsync(
                client,
                $"https://world.openproductsfacts.org/api/v0/product/{Uri.EscapeDataString(normalizedBarcode)}.json",
                normalizedBarcode,
                cancellationToken);
            if (prod is not null)
                return prod;

            return null;
        }

        private async Task<ProductLookupDto?> GetExternalProductAsync(
            HttpClient client,
            string url,
            string barcode,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                if (root.TryGetProperty("status", out var status) &&
                    status.ValueKind == JsonValueKind.Number && status.GetInt32() == 0)
                    return null;

                // Soporte UPCitemdb array "items"
                if (root.TryGetProperty("items", out var items) &&
                    items.ValueKind == JsonValueKind.Array &&
                    items.GetArrayLength() > 0)
                {
                    root = items[0];
                }
                else if (root.TryGetProperty("product", out var nestedProduct) && nestedProduct.ValueKind == JsonValueKind.Object)
                {
                    root = nestedProduct;
                }
                else if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    root = data;
                }

                var name = FirstString(root, "title", "name", "product_name_es", "product_name", "product_name_en", "generic_name_es", "generic_name");
                var brand = FirstString(root, "brand", "brands", "brand_name", "brand_owner");
                var category = FirstString(root, "category", "categories", "categories_en", "main_category");
                var description = FirstString(root, "description", "generic_name", "generic_name_en");
                var imageUrl = FirstString(root, "imageUrl", "image_url", "image_front_url", "image_small_url");
                if (string.IsNullOrWhiteSpace(imageUrl) && root.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array && images.GetArrayLength() > 0)
                {
                    imageUrl = images[0].GetString();
                }
                var quantity = FirstString(root, "quantity", "serving_size");

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description))
                    return null;

                var finalName = (name ?? description)?.Trim();
                var combinedDescription = finalName;
                if (!string.IsNullOrWhiteSpace(brand) && finalName != null && !finalName.Contains(brand, StringComparison.OrdinalIgnoreCase))
                {
                    combinedDescription = $"{finalName} · {brand.Trim()}";
                }

                return new ProductLookupDto
                {
                    Barcode = FirstString(root, "barcode", "code", "ean", "upc") ?? barcode,
                    Name = finalName,
                    Brand = brand?.Trim(),
                    Category = category?.Trim(),
                    Description = combinedDescription,
                    ImageUrl = imageUrl?.Trim(),
                    Quantity = quantity?.Trim(),
                    IsLocal = false
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "External product lookup failed for barcode {Barcode} using {Url}", barcode, url);
                return null;
            }
        }

        private static string? FirstString(JsonElement element, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (!element.TryGetProperty(propertyName, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    var first = value.EnumerateArray()
                        .FirstOrDefault(x => x.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(x.GetString()));
                    if (first.ValueKind == JsonValueKind.String)
                        return first.GetString()?.Trim();
                }
            }

            return null;
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
            existing.IsService = product.IsService;
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
            if (entity is null) return null;

            return await EnrichProductDtoAsync(_mapper.Map<ProductDto>(entity));
        }

        public async Task<IEnumerable<ProductDto>> SearchByDescriptionAsync(string description)
        {
            var entities = await _uow.Products.SearchByDescriptionAsync(description);
            var products = _mapper.Map<List<ProductDto>>(entities);
            foreach (var product in products)
                await EnrichProductDtoAsync(product);
            return products;
        }

        private async Task<ProductDto> EnrichProductDtoAsync(ProductDto product)
        {
            if (!product.CategoryId.HasValue || product.CategoryId.Value <= 0)
                return product;

            var category = await _uow.Categories.GetByIdAsync(product.CategoryId.Value);
            if (category is null)
                return product;

            product.CategoryName = category.Description;
            product.Category = new ProductCategoryDto
            {
                Id = category.Id,
                Name = category.Description,
                Description = category.Description
            };
            return product;
        }

        public Task<PagedList<ProductRow>> GetPagedListAsync(FilterPayload filterPayload, CancellationToken ct = default)
        {
            return _uow.Products.GetPagedProductsAsync(filterPayload, ct);
        }

        public async Task AddAsync(ProductDto entityDto)
        {
            if (entityDto is null) throw new ArgumentNullException(nameof(entityDto));

            bool existsBarcode = !string.IsNullOrWhiteSpace(entityDto.Barcode) &&
                await _uow.Products.ExistsByBarcodeAsync(entityDto.Barcode.Trim(), 0);
            if (existsBarcode)
                throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_BARCODE", Message = "El código de barras ya existe", Language = "ES" });

            if (string.IsNullOrWhiteSpace(entityDto.Description))
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_NAME", Message = "Product name is required", Language = "ES" });

            if (await _uow.Products.ExistsByNameAsync(entityDto.Description))
                throw new CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_NAME", Message = "Product name already exists", Language = "ES" });

            var entity = _mapper.Map<Product>(entityDto);

            // Ensure CompanyId is set from the current authenticated user's tenant context or payload fallback
            var companyId = _currentUserService.CompanyId ?? (entityDto.CompanyId > 0 ? entityDto.CompanyId : (int?)null);
            if (!companyId.HasValue || companyId.Value <= 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "COMPANY_REQUIRED", Message = "Company context is required to create products.", Language = "ES" });

            entity.CompanyId = companyId.Value;

            entity.Barcode = string.IsNullOrWhiteSpace(entity.Barcode) ? null : entity.Barcode.Trim();
            entity.ShortDescription = string.IsNullOrWhiteSpace(entity.ShortDescription) ? null : entity.ShortDescription.Trim();
            entity.Reference = string.IsNullOrWhiteSpace(entity.Reference) ? null : entity.Reference.Trim();
            entity.CreateBy = string.IsNullOrWhiteSpace(entity.CreateBy) ? (_currentUserService.UserId?.ToString() ?? "System") : entity.CreateBy;
            entity.ModifiedBy = string.IsNullOrWhiteSpace(entity.ModifiedBy) ? (_currentUserService.UserId?.ToString() ?? "System") : entity.ModifiedBy;

            if (entity.ProductTypeId <= 0)
                entity.ProductTypeId = 1;

            if (entity.ProductTypeId == 2)
                entity.InvoiceWithoutStock = true;

            if (entity.UnitOfMeasurementId <= 0)
                entity.UnitOfMeasurementId = 1;

            if (entity.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "ES" });

            if (entity.Cost < 0 || entity.Price < 0 || entity.Stock < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "Cost, Price and Stock must be >= 0", Language = "ES" });

            // Validate or fallback ProductType
            if (entity.ProductTypeId > 0)
            {
                var pt = await _uow.ProductTypes.GetByIdAsync(entity.ProductTypeId);
                if (pt == null)
                    entity.ProductTypeId = 1;
            }

            // Validate or auto-resolve Category for this company
            if (entity.CategoryId > 0)
            {
                var cat = await _uow.Categories.GetByIdAsync(entity.CategoryId);
                if (cat == null)
                {
                    var existingCat = (await _uow.Categories.FindAsync(c => c.CompanyId == entity.CompanyId && c.Active && !c.IsDeleted)).FirstOrDefault();
                    if (existingCat != null)
                    {
                        entity.CategoryId = existingCat.Id;
                    }
                    else
                    {
                        var defaultCat = new Onion.Domain.Products.Category
                        {
                            Description = "General",
                            CompanyId = entity.CompanyId,
                            Active = true,
                            IsDeleted = false,
                            CreationDate = DateTime.UtcNow,
                            CreateBy = "System",
                            ModifiedBy = "System"
                        };
                        await _uow.Categories.AddAsync(defaultCat);
                        await _uow.SaveChangesAsync();
                        entity.CategoryId = defaultCat.Id;
                    }
                }
            }
            else
            {
                var existingCat = (await _uow.Categories.FindAsync(c => c.CompanyId == entity.CompanyId && c.Active && !c.IsDeleted)).FirstOrDefault();
                if (existingCat != null)
                {
                    entity.CategoryId = existingCat.Id;
                }
                else
                {
                    var defaultCat = new Onion.Domain.Products.Category
                    {
                        Description = "General",
                        CompanyId = entity.CompanyId,
                        Active = true,
                        IsDeleted = false,
                        CreationDate = DateTime.UtcNow,
                        CreateBy = "System",
                        ModifiedBy = "System"
                    };
                    await _uow.Categories.AddAsync(defaultCat);
                    await _uow.SaveChangesAsync();
                    entity.CategoryId = defaultCat.Id;
                }
            }

            var comp = await _uow.Companies.GetByIdAsync(entity.CompanyId);
            if (comp == null) throw new CustomException(new Onion.Common.Models.Error { Code = "FK_NOT_FOUND", Message = "Company not found", Language = "ES" });

            await _uow.Products.AddAsync(entity);
            try
            {
                await _uow.SaveChangesAsync();
                entityDto.Id = entity.Id;
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

            bool existsBarcode = !string.IsNullOrWhiteSpace(entityDto.Barcode) &&
                await _uow.Products.ExistsByBarcodeAsync(entityDto.Barcode.Trim(), entityDto.Id);
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

            existing.Barcode = string.IsNullOrWhiteSpace(existing.Barcode) ? null : existing.Barcode.Trim();
            existing.ShortDescription = string.IsNullOrWhiteSpace(existing.ShortDescription) ? null : existing.ShortDescription.Trim();
            existing.Reference = string.IsNullOrWhiteSpace(existing.Reference) ? null : existing.Reference.Trim();
            existing.ModifiedBy = _currentUserService.UserId?.ToString() ?? "System";

            if (existing.ProductTypeId == 2)
                existing.InvoiceWithoutStock = true;

            if (existing.MinimumQuantity < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "MinimumQuantity must be >= 0", Language = "ES" });

            if (existing.Cost < 0 || existing.Price < 0 || existing.Stock < 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_STOCK", Message = "Cost, Price and Stock must be >= 0", Language = "ES" });

            // Validate related entities to avoid FK errors (only if product type provided)
            if (existing.ProductTypeId > 0)
            {
                var pt2 = await _uow.ProductTypes.GetByIdAsync(existing.ProductTypeId);
                if (pt2 == null)
                    existing.ProductTypeId = 1;
            }

            if (existing.CategoryId > 0)
            {
                var cat2 = await _uow.Categories.GetByIdAsync(existing.CategoryId);
                if (cat2 == null)
                {
                    var existingCat = (await _uow.Categories.FindAsync(c => c.CompanyId == existing.CompanyId && c.Active && !c.IsDeleted)).FirstOrDefault();
                    if (existingCat != null)
                        existing.CategoryId = existingCat.Id;
                }
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

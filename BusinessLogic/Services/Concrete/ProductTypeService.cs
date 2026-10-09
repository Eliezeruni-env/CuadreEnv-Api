using System;
using System.Collections.Generic;
using System.Linq;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Products;
using Onion.Common.Exceptions;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class ProductTypeService : IProductTypeService
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;

        public ProductTypeService(IUnitOfWork uow, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService, Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _uow = uow;
            _paginationService = paginationService;
            _currentUserService = currentUserService;
        }

        public async Task<ProductType> CreateAsync(ProductType entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Description)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID", Message = "Description required", Language = "EN" });

            // set CompanyId from tenant context if available; otherwise respect provided CompanyId
            var tenantCompany = _currentUserService?.CompanyId;
            if (tenantCompany.HasValue && tenantCompany.Value > 0)
            {
                entity.CompanyId = tenantCompany.Value;
            }
            else if (entity.CompanyId <= 0)
            {
                // No tenant context and no explicit company provided - fail with clear business error
                throw new CustomException(new Onion.Common.Models.Error { Code = "COMPANY_REQUIRED", Message = "CompanyId claim missing or companyId must be provided", Language = "EN" });
            }
            await _uow.ProductTypes.AddAsync(entity);
            await _uow.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.ProductTypes.GetByIdAsync(id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "ProductType not found", Language = "EN" });
            _uow.ProductTypes.Remove(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProductType>> GetAllAsync()
        {
            return await _uow.ProductTypes.ListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<ProductType>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            return await _uow.ProductTypes.GetPagedAsync(pn, ps);
        }

        public async Task<ProductType?> GetByIdAsync(int id)
        {
            return await _uow.ProductTypes.GetByIdAsync(id);
        }

        public async Task UpdateAsync(ProductType entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            var existing = await _uow.ProductTypes.GetByIdAsync(entity.Id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "ProductType not found", Language = "EN" });
            existing.Description = entity.Description;
            _uow.ProductTypes.Update(existing);
            await _uow.SaveChangesAsync();
        }
    }
}

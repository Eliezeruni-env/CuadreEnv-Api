using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Products;
using Onion.Common.Exceptions;
using System.Collections.Generic;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class ProductTypeService : IProductTypeService
    {
        private readonly IUnitOfWork _uow;

        public ProductTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ProductType> CreateAsync(ProductType entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Description)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID", Message = "Description required", Language = "EN" });

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

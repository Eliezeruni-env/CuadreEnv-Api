using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;
using Onion.Common.Exceptions;
using System.Collections.Generic;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _uow;

        public SupplierService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            if (supplier is null) throw new ArgumentNullException(nameof(supplier));
            if (string.IsNullOrWhiteSpace(supplier.Name)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_NAME", Message = "Supplier name required", Language = "EN" });

            await _uow.Suppliers.AddAsync(supplier);
            await _uow.SaveChangesAsync();
            return supplier;
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Suppliers.GetByIdAsync(id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Supplier not found", Language = "EN" });
            _uow.Suppliers.Remove(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _uow.Suppliers.ListAsync();
        }

        public async Task<Supplier?> GetByIdAsync(int id)
        {
            return await _uow.Suppliers.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            if (supplier is null) throw new ArgumentNullException(nameof(supplier));
            var existing = await _uow.Suppliers.GetByIdAsync(supplier.Id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Supplier not found", Language = "EN" });

            existing.Name = supplier.Name;
            existing.Phone = supplier.Phone;
            existing.Email = supplier.Email;

            _uow.Suppliers.Update(existing);
            await _uow.SaveChangesAsync();
        }
    }
}

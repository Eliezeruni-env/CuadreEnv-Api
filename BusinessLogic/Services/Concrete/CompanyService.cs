using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;
using Onion.Common.Exceptions;
using System.Collections.Generic;
using Onion.DataAccess.Repositories.Concrete;
using Onion.BussinesLogic.Dtos;
using System;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CompanyService : ICompanyService
    {
        private readonly IUnitOfWork _uow;

        public CompanyService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // DTO-based creation used by controllers
        public async Task<Company> CreateAsync(CreateCompanyRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Name)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_NAME", Message = "Company name required", Language = "EN" });

            var company = new Company
            {
                Name = request.Name.Trim(),
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()
            };

            await _uow.Companies.AddAsync(company);
            await _uow.SaveChangesAsync(); // ensure Id assigned

            // create default settings and link
            var settings = new CompanySettings
            {
                CompanyId = company.Id,
                CreditDaysLimit = 30,
                BlockSalesIfOverdue = false
            };
            await _uow.CompanySettingsRepo.AddAsync(settings);
            await _uow.SaveChangesAsync();

            company.Settings = settings;
            return company;
        }

        // Preserve existing entity-based creation
        public async Task<Company> CreateAsync(Company company)
        {
            if (company is null) throw new ArgumentNullException(nameof(company));
            if (string.IsNullOrWhiteSpace(company.Name)) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_NAME", Message = "Company name required", Language = "EN" });

            await _uow.Companies.AddAsync(company);
            await _uow.SaveChangesAsync();

            // create default settings if missing
            if (company.Settings == null)
            {
                var settings = new CompanySettings
                {
                    CompanyId = company.Id,
                    CreditDaysLimit = 30,
                    BlockSalesIfOverdue = false
                };
                await _uow.CompanySettingsRepo.AddAsync(settings);
                await _uow.SaveChangesAsync();
                company.Settings = settings;
            }

            return company;
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Companies.GetByIdAsync(id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Company not found", Language = "EN" });
            _uow.Companies.Remove(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<Company>> GetAllAsync()
        {
            return await _uow.Companies.ListAsync();
        }

        public async Task<Company?> GetByIdAsync(int id)
        {
            return await _uow.Companies.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Company company)
        {
            if (company is null) throw new ArgumentNullException(nameof(company));
            var existing = await _uow.Companies.GetByIdAsync(company.Id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Company not found", Language = "EN" });

            existing.Name = company.Name;
            existing.Address = company.Address;
            existing.Phone = company.Phone;

            _uow.Companies.Update(existing);
            await _uow.SaveChangesAsync();
        }
    }
}

using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CompanySettingsService : ICompanySettingsService
    {
        private readonly IRepository<CompanySettings> _repo;
        private readonly Onion.DataAccess.OnionDbContext _db;

        public CompanySettingsService(IRepository<CompanySettings> repo, Onion.DataAccess.OnionDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        public async Task<Onion.BussinesLogic.Dtos.CompanySettingsEditableDto?> GetSettingsAsync(int companyId)
        {
            var settings = await _db.CompanySettings.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.CompanyId == companyId);
            var company = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId);

            if (settings == null && company == null)
                return null;

            return new CompanySettingsEditableDto
            {
                CompanyName = company?.Name ?? settings?.CommercialName ?? string.Empty,
                CommercialName = settings?.CommercialName ?? company?.Name ?? string.Empty,
                Rnc = company?.Rnc ?? string.Empty,
                Phone = company?.Phone ?? string.Empty,
                Email = company?.Email ?? string.Empty,
                Address = company?.Address ?? string.Empty,
                Website = settings?.LogoUrl,
                LogoUrl = settings?.LogoUrl,
                InvoiceFooterPhrase = settings?.InvoiceFooterPhrase ?? string.Empty,
                TicketPaperSize = settings?.TicketPaperSize ?? "80mm",
                Currency = string.IsNullOrWhiteSpace(settings?.Currency) ? "DOP" : settings.Currency,
                DefaultTaxPercentage = settings?.DefaultTaxPercentage ?? 18m,
                AllowNegativeStock = settings?.AllowNegativeStock ?? false,
                TicketInstagram = settings?.TicketInstagram ?? string.Empty,
                TicketWhatsapp = settings?.TicketWhatsapp ?? string.Empty,
                TicketShowQr = settings?.TicketShowQr ?? true
            };
        }

        public async Task UpdateSettingsAsync(int companyId, UpdateCompanySettingsDto dto)
        {
            var s = await _db.CompanySettings.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.CompanyId == companyId);
            var isNew = false;
            if (s == null)
            {
                isNew = true;
                s = new CompanySettings
                {
                    CompanyId = companyId,
                    CreationDate = DateTime.UtcNow,
                    Active = true,
                    IsDeleted = false
                };
            }

            s.CommercialName = !string.IsNullOrWhiteSpace(dto.CommercialName) ? dto.CommercialName : dto.CompanyName;
            s.LogoUrl = dto.LogoUrl;
            s.InvoiceFooterPhrase = dto.InvoiceFooterPhrase ?? string.Empty;
            s.TicketPaperSize = !string.IsNullOrWhiteSpace(dto.TicketPaperSize) ? dto.TicketPaperSize : "80mm";
            if (!string.IsNullOrWhiteSpace(dto.Currency)) s.Currency = dto.Currency;
            if (dto.DefaultTaxPercentage >= 0) s.DefaultTaxPercentage = dto.DefaultTaxPercentage;
            s.AllowNegativeStock = dto.AllowNegativeStock;
            s.TicketInstagram = dto.TicketInstagram;
            s.TicketWhatsapp = dto.TicketWhatsapp;
            s.TicketShowQr = dto.TicketShowQr;
            s.ModificationDate = DateTime.UtcNow;

            if (isNew)
            {
                await _db.CompanySettings.AddAsync(s);
            }
            else
            {
                _db.CompanySettings.Update(s);
            }

            var company = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId);
            if (company != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.CompanyName)) company.Name = dto.CompanyName;
                if (!string.IsNullOrWhiteSpace(dto.Rnc)) company.Rnc = dto.Rnc;
                if (!string.IsNullOrWhiteSpace(dto.Phone)) company.Phone = dto.Phone;
                if (!string.IsNullOrWhiteSpace(dto.Email)) company.Email = dto.Email;
                if (!string.IsNullOrWhiteSpace(dto.Address)) company.Address = dto.Address;
                _db.Companies.Update(company);
            }

            await _db.SaveChangesAsync();
        }
    }
}

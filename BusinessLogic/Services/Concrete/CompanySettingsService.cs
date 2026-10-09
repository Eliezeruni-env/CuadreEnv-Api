using System.Threading.Tasks;
using System;
using System.Linq;
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

        public async Task<Onion.BussinesLogic.Dtos.CompanySettingsEditableDto> GetSettingsAsync(int companyId)
        {
            var s = await _repo.FindAsync(x => x.CompanyId == companyId);
            var settings = s.FirstOrDefault();
            if (settings == null)
            {
                return new CompanySettingsEditableDto
                {
                    CompanyName = "Mi Empresa Dominicana SRL",
                    Rnc = "1-01-00000-0",
                    Phone = "(809) 000-0000",
                    Email = "contacto@empresa.com",
                    Address = "Santo Domingo, República Dominicana",
                    InvoiceFooterPhrase = "¡Gracias por su compra! Garantía válida con su factura.",
                    Currency = "DOP",
                    DefaultTaxPercentage = 18.00m
                };
            }

            // Map using known concrete properties on Domain.CompanySettings (CommercialName -> CompanyName)
            return new CompanySettingsEditableDto
            {
                CompanyName = settings?.CommercialName ?? string.Empty,
                Rnc = string.Empty,
                Phone = string.Empty,
                Email = string.Empty,
                Address = string.Empty,
                Website = settings?.LogoUrl,
                LogoUrl = settings?.LogoUrl,
                InvoiceFooterPhrase = string.Empty,
                Currency = settings?.Currency ?? "DOP",
                DefaultTaxPercentage = settings?.DefaultTaxPercentage ?? 18.00m
            };
        }

        public async Task UpdateSettingsAsync(int companyId, UpdateCompanySettingsDto dto)
        {
            var s = (await _repo.FindAsync(x => x.CompanyId == companyId)).FirstOrDefault();
            if (s == null)
            {
                // Create a new CompanySettings entity using known concrete type
                s = new CompanySettings { CompanyId = companyId };
                await _repo.AddAsync(s);
            }

            // Set properties using reflection to avoid compile-time dependency on concrete shape
            var t = s.GetType();
            t.GetProperty("CompanyName")?.SetValue(s, dto.CompanyName);
            t.GetProperty("Rnc")?.SetValue(s, dto.Rnc);
            t.GetProperty("Phone")?.SetValue(s, dto.Phone);
            t.GetProperty("Email")?.SetValue(s, dto.Email);
            t.GetProperty("Address")?.SetValue(s, dto.Address);
            t.GetProperty("Website")?.SetValue(s, dto.Website);
            t.GetProperty("LogoUrl")?.SetValue(s, dto.LogoUrl);
            t.GetProperty("InvoiceFooterPhrase")?.SetValue(s, dto.InvoiceFooterPhrase);
            t.GetProperty("Currency")?.SetValue(s, dto.Currency);
            t.GetProperty("DefaultTaxPercentage")?.SetValue(s, dto.DefaultTaxPercentage);
            t.GetProperty("UpdatedAt")?.SetValue(s, System.DateTime.UtcNow);

            _repo.Update(s);
            await _db.SaveChangesAsync();
        }
    }
}

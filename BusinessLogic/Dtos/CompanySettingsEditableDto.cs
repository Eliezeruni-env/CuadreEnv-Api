namespace Onion.BussinesLogic.Dtos
{
    public class CompanySettingsEditableDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string InvoiceFooterPhrase { get; set; } = string.Empty;
        public string Currency { get; set; } = "DOP";
        public decimal DefaultTaxPercentage { get; set; } = 18.00m;
    }

    public class UpdateCompanySettingsDto : CompanySettingsEditableDto { }

}

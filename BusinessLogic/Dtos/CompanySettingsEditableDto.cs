namespace Onion.BussinesLogic.Dtos
{
    public class CompanySettingsEditableDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? CommercialName { get; set; }
        public string Rnc { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string InvoiceFooterPhrase { get; set; } = string.Empty;
        public string? TicketPaperSize { get; set; } = "80mm";
        public string Currency { get; set; } = string.Empty;
        public decimal DefaultTaxPercentage { get; set; }
        public bool AllowNegativeStock { get; set; }
        public string? TicketInstagram { get; set; }
        public string? TicketWhatsapp { get; set; }
        public bool TicketShowQr { get; set; } = true;
    }

    public class UpdateCompanySettingsDto : CompanySettingsEditableDto { }
}

namespace Onion.BussinesLogic.Dtos
{
    public record CompanyDto(
        int Id,
        string Name,
        string? Address,
        string? Phone,
        CompanySettingsDto? Settings
    );

    public record CompanySettingsDto(
        int Id,
        int CompanyId,
        int CreditDaysLimit,
        bool BlockSalesIfOverdue,
        string Currency,
        string TimeZone,
        string InvoiceNumberFormat,
        string? LogoUrl,
        string? CommercialName,
        int DefaultStockAlertThreshold,
        decimal DefaultTaxPercentage
    );

    public record CreateCompanyDto(string Name, string? Address, string? Phone);

}

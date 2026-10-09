using Onion.Domain.Finance;

namespace Onion.BussinesLogic.Services.Abstract;

public interface ITaxpayerService
{
    Task<TaxpayerDto?> GetAsync(string rncOrCedula, CancellationToken cancellationToken = default);
    Task<TaxpayerDto> UpsertAsync(Taxpayer taxpayer, CancellationToken cancellationToken = default);
}

public sealed record TaxpayerDto(string Rnc, string BusinessName, string? CommercialName, string? Category, string Status);

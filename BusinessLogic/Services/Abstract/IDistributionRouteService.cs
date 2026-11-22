using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IDistributionRouteService
    {
        Task<IEnumerable<DistributionRouteDto>> GetRoutesAsync(string? status, string? search, DateTime? date, CancellationToken ct = default);
        Task<DistributionRouteDto?> GetRouteByIdAsync(int id, CancellationToken ct = default);
        Task<DistributionRouteDto> CreateRouteAsync(CreateDistributionRouteRequest request, CancellationToken ct = default);
        Task<DistributionRouteDto> LoadItemsAsync(int id, LoadRouteItemsRequest request, CancellationToken ct = default);
        Task<DistributionRouteDto> RegisterMobileSaleAsync(int id, MobileRouteSaleRequest request, CancellationToken ct = default);
        Task<DistributionRouteDto> LiquidateRouteAsync(int id, LiquidateRouteRequest request, CancellationToken ct = default);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.Domain.Distribution;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class DistributionRouteService : IDistributionRouteService
    {
        private readonly OnionDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DistributionRouteService> _logger;

        public DistributionRouteService(
            OnionDbContext db,
            ICurrentUserService currentUser,
            ILogger<DistributionRouteService> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _logger = logger;
        }

        private int GetCompanyId()
        {
            var companyId = _currentUser.CompanyId ?? _db.TenantCompanyId;
            if (!companyId.HasValue || companyId.Value <= 0)
                throw new InvalidOperationException("Empresa no autenticada o inválida.");
            return companyId.Value;
        }

        public async Task<IEnumerable<DistributionRouteDto>> GetRoutesAsync(string? status, string? search, DateTime? date, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var query = _db.DistributionRoutes
                .Include(r => r.Items)
                .Where(r => r.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("TODOS", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<DistributionRouteStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(r => r.Status == parsedStatus);
                }
                else if (status.Equals("Cargando", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Status == DistributionRouteStatus.CARGANDO);
                }
                else if (status.Equals("En reparto", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Status == DistributionRouteStatus.EN_REPARTO);
                }
                else if (status.Equals("Liquidada", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Status == DistributionRouteStatus.LIQUIDADA);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(r => r.RouteName.ToLower().Contains(s) ||
                                         r.RouteCode.ToLower().Contains(s) ||
                                         r.DriverName.ToLower().Contains(s) ||
                                         r.VehicleName.ToLower().Contains(s) ||
                                         r.CoverageZone.ToLower().Contains(s));
            }

            if (date.HasValue)
            {
                var d = date.Value.Date;
                var nextD = d.AddDays(1);
                query = query.Where(r => r.Date >= d && r.Date < nextD);
            }

            var routes = await query.OrderByDescending(r => r.Id).ToListAsync(ct);
            return routes.Select(MapToDto);
        }

        public async Task<DistributionRouteDto?> GetRouteByIdAsync(int id, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var route = await _db.DistributionRoutes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);

            return route == null ? null : MapToDto(route);
        }

        public async Task<DistributionRouteDto> CreateRouteAsync(CreateDistributionRouteRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var count = await _db.DistributionRoutes.CountAsync(r => r.CompanyId == companyId, ct);
            var code = $"RUT-{(count + 1).ToString("D4")}";

            var route = new DistributionRoute
            {
                CompanyId = companyId,
                RouteCode = code,
                RouteName = request.RouteName,
                CoverageZone = request.CoverageZone,
                DriverName = request.DriverName,
                VehiclePlate = request.VehiclePlate,
                VehicleName = request.VehicleName,
                Date = DateTime.UtcNow,
                Status = DistributionRouteStatus.CARGANDO,
                OriginWarehouse = request.OriginWarehouse ?? "Almacén Principal",
                OriginWarehouseId = request.OriginWarehouseId,
                TotalCollectedCash = 0,
                TotalCollectedTransfer = 0,
                TotalCreditSales = 0,
                CreatedAt = DateTime.UtcNow
            };

            if (request.Items != null && request.Items.Count > 0)
            {
                foreach (var item in request.Items)
                {
                    route.Items.Add(new DistributionRouteItem
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        LoadedQuantity = item.LoadedQuantity,
                        SoldQuantity = 0,
                        ReturnedQuantity = 0,
                        UnitPrice = item.UnitPrice,
                        TotalSoldAmount = 0,
                        TotalRemainingValue = item.LoadedQuantity * item.UnitPrice
                    });
                }
            }

            _db.DistributionRoutes.Add(route);
            await _db.SaveChangesAsync(ct);

            return MapToDto(route);
        }

        public async Task<DistributionRouteDto> LoadItemsAsync(int id, LoadRouteItemsRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var route = await _db.DistributionRoutes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);

            if (route == null)
                throw new KeyNotFoundException($"Ruta {id} no encontrada.");

            if (!string.IsNullOrWhiteSpace(request.OriginWarehouse))
            {
                route.OriginWarehouse = request.OriginWarehouse;
            }

            foreach (var loadItem in request.Items)
            {
                var existing = route.Items.FirstOrDefault(i => i.ProductId == loadItem.ProductId);
                if (existing != null)
                {
                    existing.LoadedQuantity += loadItem.LoadedQuantity;
                    existing.TotalRemainingValue = (existing.LoadedQuantity - existing.SoldQuantity) * existing.UnitPrice;
                }
                else
                {
                    route.Items.Add(new DistributionRouteItem
                    {
                        DistributionRouteId = route.Id,
                        ProductId = loadItem.ProductId,
                        ProductCode = loadItem.ProductCode,
                        ProductName = loadItem.ProductName,
                        LoadedQuantity = loadItem.LoadedQuantity,
                        SoldQuantity = 0,
                        ReturnedQuantity = 0,
                        UnitPrice = loadItem.UnitPrice,
                        TotalSoldAmount = 0,
                        TotalRemainingValue = loadItem.LoadedQuantity * loadItem.UnitPrice
                    });
                }
            }

            if (route.Status == DistributionRouteStatus.CARGANDO)
            {
                route.Status = DistributionRouteStatus.EN_REPARTO;
            }

            await _db.SaveChangesAsync(ct);
            return MapToDto(route);
        }

        public async Task<DistributionRouteDto> RegisterMobileSaleAsync(int id, MobileRouteSaleRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var route = await _db.DistributionRoutes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);

            if (route == null)
                throw new KeyNotFoundException($"Ruta {id} no encontrada.");

            var pm = request.PaymentMethod?.ToLowerInvariant() ?? "efectivo";
            if (pm.Contains("transf"))
            {
                route.TotalCollectedTransfer += request.Total;
            }
            else if (pm.Contains("cred") || pm.Contains("créd"))
            {
                route.TotalCreditSales += request.Total;
            }
            else
            {
                route.TotalCollectedCash += request.Total;
            }

            foreach (var saleItem in request.Items)
            {
                var routeItem = route.Items.FirstOrDefault(i => i.ProductId == saleItem.ProductId);
                if (routeItem != null)
                {
                    routeItem.SoldQuantity += saleItem.Quantity;
                    routeItem.TotalSoldAmount += saleItem.Subtotal;
                    routeItem.TotalRemainingValue = Math.Max(0, (routeItem.LoadedQuantity - routeItem.SoldQuantity) * routeItem.UnitPrice);
                }
            }

            if (route.Status == DistributionRouteStatus.CARGANDO)
            {
                route.Status = DistributionRouteStatus.EN_REPARTO;
            }

            await _db.SaveChangesAsync(ct);
            return MapToDto(route);
        }

        public async Task<DistributionRouteDto> LiquidateRouteAsync(int id, LiquidateRouteRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var route = await _db.DistributionRoutes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);

            if (route == null)
                throw new KeyNotFoundException($"Ruta {id} no encontrada.");

            route.Status = DistributionRouteStatus.LIQUIDADA;
            route.ActualCash = request.ActualCash;
            route.ExpectedCash = request.ExpectedCash > 0 ? request.ExpectedCash : route.TotalCollectedCash;
            route.CashDifference = route.ActualCash - route.ExpectedCash;
            route.CashRegisterId = request.CashRegisterId;
            route.DestinationWarehouseId = request.DestinationWarehouseId;
            route.DestinationWarehouse = request.DestinationWarehouse ?? "Almacén Principal";
            route.LiquidatedAt = DateTime.UtcNow;

            if (request.ReturnedItems != null)
            {
                foreach (var ret in request.ReturnedItems)
                {
                    var item = route.Items.FirstOrDefault(i => i.ProductId == ret.ProductId);
                    if (item != null)
                    {
                        item.ReturnedQuantity = ret.ReturnedQuantity;
                    }
                }
            }
            else
            {
                foreach (var item in route.Items)
                {
                    item.ReturnedQuantity = Math.Max(0, item.LoadedQuantity - item.SoldQuantity);
                }
            }

            await _db.SaveChangesAsync(ct);
            return MapToDto(route);
        }

        private static DistributionRouteDto MapToDto(DistributionRoute route)
        {
            string statusStr = route.Status switch
            {
                DistributionRouteStatus.CARGANDO => "Cargando",
                DistributionRouteStatus.EN_REPARTO => "En reparto",
                DistributionRouteStatus.LIQUIDADA => "Liquidada",
                _ => "Cargando"
            };

            return new DistributionRouteDto
            {
                Id = route.Id,
                RouteCode = route.RouteCode,
                RouteName = route.RouteName,
                CoverageZone = route.CoverageZone,
                DriverName = route.DriverName,
                VehiclePlate = route.VehiclePlate,
                VehicleName = route.VehicleName,
                Date = route.Date.ToString("yyyy-MM-dd"),
                Status = statusStr,
                TotalCollectedCash = route.TotalCollectedCash,
                TotalCollectedTransfer = route.TotalCollectedTransfer,
                TotalCreditSales = route.TotalCreditSales,
                OriginWarehouse = route.OriginWarehouse,
                DestinationWarehouse = route.DestinationWarehouse,
                Items = route.Items.Select(i => new RouteLoadItemDto
                {
                    ProductId = i.ProductId,
                    ProductCode = i.ProductCode,
                    ProductName = i.ProductName,
                    LoadedQuantity = i.LoadedQuantity,
                    SoldQuantity = i.SoldQuantity,
                    ReturnedQuantity = i.ReturnedQuantity,
                    UnitPrice = i.UnitPrice,
                    TotalSoldAmount = i.TotalSoldAmount,
                    TotalRemainingValue = i.TotalRemainingValue
                }).ToList()
            };
        }
    }
}

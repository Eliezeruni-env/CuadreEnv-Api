using System;
using System.Collections.Generic;
using Onion.Domain.Distribution;

namespace Onion.BussinesLogic.Dtos
{
    public class RouteLoadItemDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal LoadedQuantity { get; set; }
        public decimal SoldQuantity { get; set; }
        public decimal ReturnedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalSoldAmount { get; set; }
        public decimal TotalRemainingValue { get; set; }
    }

    public class DistributionRouteDto
    {
        public int Id { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string CoverageZone { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = "Cargando"; // 'Cargando' | 'En reparto' | 'Liquidada'
        public List<RouteLoadItemDto> Items { get; set; } = new List<RouteLoadItemDto>();
        public decimal TotalCollectedCash { get; set; }
        public decimal TotalCollectedTransfer { get; set; }
        public decimal TotalCreditSales { get; set; }
        public string OriginWarehouse { get; set; } = string.Empty;
        public string? DestinationWarehouse { get; set; }
    }

    public class CreateDistributionRouteRequest
    {
        public string RouteName { get; set; } = string.Empty;
        public string CoverageZone { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string OriginWarehouse { get; set; } = "Almacén Principal";
        public int? OriginWarehouseId { get; set; }
        public List<RouteLoadItemDto>? Items { get; set; }
    }

    public class LoadRouteItemsRequest
    {
        public List<RouteLoadItemDto> Items { get; set; } = new List<RouteLoadItemDto>();
        public string? OriginWarehouse { get; set; }
    }

    public class MobileRouteSaleRequest
    {
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = "Consumidor Final";
        public string PaymentMethod { get; set; } = "Efectivo"; // Efectivo, Transferencia, Crédito
        public decimal Total { get; set; }
        public List<RouteSaleDetailRequest> Items { get; set; } = new List<RouteSaleDetailRequest>();
    }

    public class RouteSaleDetailRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class LiquidateRouteRequest
    {
        public decimal ActualCash { get; set; }
        public decimal ExpectedCash { get; set; }
        public int? CashRegisterId { get; set; }
        public int? DestinationWarehouseId { get; set; }
        public string? DestinationWarehouse { get; set; }
        public List<RouteLoadItemDto>? ReturnedItems { get; set; }
    }
}

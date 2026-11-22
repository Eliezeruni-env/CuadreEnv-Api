using System;
using System.Collections.Generic;

namespace Onion.Domain.Distribution
{
    public enum DistributionRouteStatus
    {
        CARGANDO = 0,
        EN_REPARTO = 1,
        LIQUIDADA = 2
    }

    public class DistributionRoute : BaseEntity
    {
        public int CompanyId { get; set; }
        public string RouteCode { get; set; } = string.Empty; // e.g. RUT-SUR-001
        public string RouteName { get; set; } = string.Empty; // e.g. Ruta Sur
        public string CoverageZone { get; set; } = string.Empty; // e.g. Zona Metropolitana / Sur
        public string DriverName { get; set; } = string.Empty; // e.g. Carlos Méndez
        public string VehiclePlate { get; set; } = string.Empty; // e.g. L-409182
        public string VehicleName { get; set; } = string.Empty; // e.g. Camión F-023
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public DistributionRouteStatus Status { get; set; } = DistributionRouteStatus.CARGANDO;

        public int? OriginWarehouseId { get; set; }
        public string OriginWarehouse { get; set; } = "Almacén Principal";
        public int? DestinationWarehouseId { get; set; }
        public string? DestinationWarehouse { get; set; }

        public decimal TotalCollectedCash { get; set; }
        public decimal TotalCollectedTransfer { get; set; }
        public decimal TotalCreditSales { get; set; }

        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal CashDifference { get; set; }
        public int? CashRegisterId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LiquidatedAt { get; set; }
        public string? Notes { get; set; }

        public List<DistributionRouteItem> Items { get; set; } = new List<DistributionRouteItem>();
    }

    public class DistributionRouteItem : BaseEntity
    {
        public int DistributionRouteId { get; set; }
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
}

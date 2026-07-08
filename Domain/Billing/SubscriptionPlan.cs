using System;
using Onion.Domain;

namespace Onion.Domain.Billing
{
    public class SubscriptionPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int MaxUsers { get; set; }
        public int MaxWarehouses { get; set; }
        public int MaxProducts { get; set; }
        public int MaxSalesPerMonth { get; set; }
        public decimal Price { get; set; }
        public string Features { get; set; } = string.Empty; // comma-separated feature keys
    }
}

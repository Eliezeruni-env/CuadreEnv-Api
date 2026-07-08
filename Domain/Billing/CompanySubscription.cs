using System;
using Onion.Domain;

namespace Onion.Domain.Billing
{
    public enum SubscriptionStatus { Active, PastDue, Cancelled, Trialing }

    public class CompanySubscription : BaseEntity
    {
        public int CompanyId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public string? ExternalCustomerId { get; set; }
        public string? ExternalSubscriptionId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? RenewalDate { get; set; }
        public SubscriptionStatus Status { get; set; }
    }
}

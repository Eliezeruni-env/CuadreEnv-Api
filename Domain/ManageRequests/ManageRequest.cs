using System;
using System.Collections.Generic;
using Onion.Domain;

namespace Onion.Domain.ManageRequests
{
    public enum ManageRequestType { PurchaseReceipt = 1, InventoryAdjustment = 2, Cancellation = 3 }

    public enum ManageRequestStatus { Pending = 1, Approved = 2, Rejected = 3 }

    public class ManageRequest : BaseEntity
    {
        public ManageRequestType Type { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
        public ManageRequestStatus Status { get; set; } = ManageRequestStatus.Pending;
        public string? Comment { get; set; }
        public int CompanyId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<ManageRequestTimeline> Timeline { get; set; } = new List<ManageRequestTimeline>();
    }

    public class ManageRequestTimeline : BaseEntity
    {
        public int ManageRequestId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public int OldStatus { get; set; }
        public int NewStatus { get; set; }
    }
}

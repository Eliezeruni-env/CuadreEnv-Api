using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    public class CreditNoteDto
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Reason { get; set; }
        public bool ReturnToStock { get; set; }
        public List<CreditNoteDetailDto> Details { get; set; } = new List<CreditNoteDetailDto>();
    }

    public class CreditNoteDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal OriginalQuantity { get; set; }
        public decimal CreditedQuantity { get; set; }
    }

    public class CreateCreditNoteRequest
    {
        public int? BillingId { get; set; }
        public int? SaleId { get; set; }
        public string? BillingNumber { get; set; }
        public int CustomerId { get; set; }
        public string? Reason { get; set; }
        public string? RefundMethod { get; set; }
        public int? CreditNoteType { get; set; }
        public int? WarehouseId { get; set; }
        public bool ReturnToStock { get; set; } = true;

        public List<CreateCreditNoteDetail>? Details { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("creditNoteDetails")]
        public List<CreateCreditNoteDetail>? CreditNoteDetails { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("items")]
        public List<CreateCreditNoteDetail>? Items { get; set; }

        public List<CreateCreditNoteDetail> GetResolvedDetails()
        {
            if (Details != null && Details.Count > 0)
                return Details;
            if (CreditNoteDetails != null && CreditNoteDetails.Count > 0)
                return CreditNoteDetails;
            if (Items != null && Items.Count > 0)
                return Items;
            return new List<CreateCreditNoteDetail>();
        }
    }

    public class CreateCreditNoteDetail
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int? WarehouseId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("price")]
        public decimal? Price
        {
            get => UnitPrice;
            set { if (value.HasValue && UnitPrice == 0) UnitPrice = value.Value; }
        }
    }
}

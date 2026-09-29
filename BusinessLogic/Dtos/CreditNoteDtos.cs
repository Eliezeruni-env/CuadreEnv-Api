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
        public List<CreditNoteDetailDto> Details { get; set; } = new List<CreditNoteDetailDto>();
    }

    public class CreditNoteDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CreateCreditNoteRequest
    {
        public int CustomerId { get; set; }
        public string? Reason { get; set; }
        public List<CreateCreditNoteDetail> Details { get; set; } = new List<CreateCreditNoteDetail>();
    }

    public class CreateCreditNoteDetail
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

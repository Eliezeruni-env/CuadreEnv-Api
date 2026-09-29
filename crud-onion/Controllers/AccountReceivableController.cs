
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Onion.Domain;
using Onion.Common.Exceptions;
using Onion.Common.Models;
using Onion.Common.Services;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Finance;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountReceivableController : ControllerBase
    {
        private readonly IAccountReceivableService _service;
        private readonly IGlobalizationService _globalization;
        private readonly IUnitOfWork _uow;
        private readonly ISaleService _saleService;

        public AccountReceivableController(IAccountReceivableService service, IGlobalizationService globalization, IUnitOfWork uow, ISaleService saleService)
        {
            _service = service;
            _globalization = globalization;
            _uow = uow;
            _saleService = saleService;
        }

        // GET /v1/AccountReceivable
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null, [FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var list = (await _uow.AccountReceivables.ListAsync()).ToList();

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<ReceivableStatus>(status, true, out var st))
                    list = list.Where(a => a.Status == st).ToList();
            }

            // Search by customer name, identification or sale id
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                // load customers to filter
                var customers = (await _uow.Customers.ListAsync()).ToList();
                list = list.Where(a =>
                    (a.SaleId.HasValue && a.SaleId.Value.ToString() == s) ||
                    (a.CustomerId.HasValue && (customers.FirstOrDefault(c => c.Id == a.CustomerId.Value)?.Name ?? string.Empty).ToLowerInvariant().Contains(s))
                ).ToList();
            }

            var total = list.Count;
            if (pageNumber.HasValue)
            {
                var pn = Math.Max(1, pageNumber.Value);
                var ps = pageSize.HasValue ? Math.Clamp(pageSize.Value, 1, 100) : 20;
                var items = list.Skip((pn - 1) * ps).Take(ps).Select(a => new
                {
                    id = a.Id,
                    companyId = a.CompanyId,
                    customerId = a.CustomerId,
                    saleId = a.SaleId,
                    totalAmount = a.TotalAmount,
                    paidAmount = a.PaidAmount,
                    pendingAmount = a.Balance,
                    status = a.Status.ToString(),
                    dueDate = a.DueDate
                }).ToList();
                return Ok(ApiResponse<object>.Ok(new { items, pageNumber = pn, pageSize = ps, total }));
            }

            var all = list.Select(a => new
            {
                id = a.Id,
                companyId = a.CompanyId,
                customerId = a.CustomerId,
                saleId = a.SaleId,
                totalAmount = a.TotalAmount,
                paidAmount = a.PaidAmount,
                pendingAmount = a.Balance,
                status = a.Status.ToString(),
                dueDate = a.DueDate
            }).ToList();

            return Ok(ApiResponse<object>.Ok(all));
        }

        [HttpGet("due-soon")]
        public async Task<IActionResult> GetDueSoon([FromQuery] int days = 7)
        {
            var items = await _service.GetDueSoonAsync(days);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ar = await _uow.AccountReceivables.GetByIdAsync(id);
            if (ar == null) return NotFound(ApiResponse<object>.Fail("AccountReceivable not found"));

            // Load customer
            Onion.Domain.Customer? customer = null;
            if (ar.CustomerId.HasValue)
                customer = await _uow.Customers.GetByIdAsync(ar.CustomerId.Value);

            // Load payments for related sale
            var payments = new System.Collections.Generic.List<object>();
            if (ar.SaleId.HasValue)
            {
                var payList = (await _uow.Payments.FindAsync(p => p.SaleId == ar.SaleId.Value)).ToList();
                payments = payList.Select(p => new { id = p.Id, amount = p.Amount, date = p.CreationDate, method = p.PaymentMethod.ToString(), reference = p.Reference }).ToList<object>();
            }

            var dto = new
            {
                id = ar.Id,
                companyId = ar.CompanyId,
                customer = customer == null ? null : new { id = customer.Id, name = customer.Name, email = customer.Email, phone = customer.Phone },
                saleId = ar.SaleId,
                totalAmount = ar.TotalAmount,
                paidAmount = ar.PaidAmount,
                pendingAmount = ar.Balance,
                status = ar.Status.ToString(),
                dueDate = ar.DueDate,
                payments = payments
            };

            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(dto));
        }

        // POST /v1/AccountReceivable
        [HttpPost]
        public async Task<IActionResult> CreateReceivable([FromBody] CreateReceivableRequest req)
        {
            if (req == null) return BadRequest(Onion.Common.Models.ApiResponse<object>.Fail("Body required"));

            // If client provided sale details in the receivable creation, create the sale first
            int? createdSaleId = req.SaleId;
            if ((createdSaleId == null || createdSaleId == 0) && req.Details != null && req.Details.Any())
            {
                var sale = new Sale
                {
                    CompanyId = req.CompanyId,
                    CustomerId = req.CustomerId,
                    Total = req.TotalAmount,
                    PaidAmount = req.PaidAmount,
                    DueDate = req.DueDate,
                    PaymentType = req.PaidAmount >= req.TotalAmount ? PaymentType.CASH : PaymentType.CREDIT,
                    Details = req.Details.Select(d => new SaleDetail { ProductId = d.ProductId, Quantity = d.Quantity, UnitPrice = d.UnitPrice }).ToList()
                };

                var createdSale = await _saleService.CreateAsync(sale);
                createdSaleId = createdSale.Id;

                // If there is an initial payment, record it using sale payment flow (this will also create a cash movement if cashRegisterId provided)
                if (req.PaidAmount > 0 && req.Payment != null)
                {
                    var pay = new Payment { CompanyId = req.CompanyId, Amount = req.PaidAmount, CreateBy = req.Payment.Reference };
                    await _saleService.AddPaymentAsync(createdSale.Id, pay, req.Payment.Method, req.Payment.CashRegisterId);
                }
            }

            var ar = new Onion.Domain.Finance.AccountReceivable
            {
                CompanyId = req.CompanyId,
                CustomerId = req.CustomerId,
                SaleId = createdSaleId,
                TotalAmount = req.TotalAmount,
                PaidAmount = req.PaidAmount,
                DueDate = req.DueDate ?? DateTime.UtcNow.AddDays(30),
                Status = req.PaidAmount >= req.TotalAmount ? ReceivableStatus.Paid : ReceivableStatus.Open
            };

            Onion.Domain.Finance.PaymentPlan? plan = null;
            if (req.Plan != null)
            {
                // Parse frequency string to enum, default to Monthly
                var freq = PaymentFrequency.Monthly;
                if (!string.IsNullOrWhiteSpace(req.Plan.Frequency))
                {
                    Enum.TryParse<PaymentFrequency>(req.Plan.Frequency, true, out freq);
                }

                plan = new PaymentPlan
                {
                    InstallmentAmount = req.Plan.InstallmentAmount,
                    TotalInstallments = req.Plan.TotalInstallments,
                    Frequency = freq,
                    StartDate = req.Plan.StartsAt ?? DateTime.UtcNow
                };
            }

            var created = await _service.CreateWithPlanAsync(ar, plan);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<object>.Ok(created, "Account receivable created"));
        }

        [HttpGet("{id}/installments")]
        public async Task<IActionResult> GetInstallments(int id)
        {
            // find payment plan for this receivable
            var plans = (await _uow.PaymentPlans.FindAsync(p => p.AccountReceivableId == id)).ToList();
            if (!plans.Any()) return Ok(ApiResponse<object>.Ok(new List<object>()));
            var plan = plans.First();
            var installments = await _service.GetInstallmentsAsync(plan.Id);
            return Ok(ApiResponse<object>.Ok(installments));
        }

        [HttpPost("{id}/installments/{installmentId}/pay")]
        public async Task<IActionResult> PayInstallment(int id, int installmentId, [FromBody] PaymentRequest req)
        {
            if (req.Amount <= 0) return BadRequest(ApiResponse<object>.Fail("Amount must be greater than zero"));
            await _service.PayInstallmentAsync(installmentId, req.Amount);

            // Optionally record CashMovement if method is cash
            if (req.Method != null && req.Method.Equals("CASH", StringComparison.OrdinalIgnoreCase) && req.Amount > 0)
            {
                var cm = new Onion.Domain.CashMovement { CompanyId = 0, Amount = req.Amount, Description = $"Installment payment {installmentId}", CreateBy = "system" };
                await _uow.CashMovements.AddAsync(cm);
                await _uow.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpPost("{id}/payments")]
        public async Task<IActionResult> RegisterPayment(int id, [FromBody] PaymentRequest req)
        {
            // Validate amount
            if (req.Amount <= 0) return BadRequest(ApiResponse<object>.Fail("Amount must be greater than zero"));

            // Fetch receivable to determine linked sale
            var ar = await _uow.AccountReceivables.GetByIdAsync(id) ?? throw new CustomException(new Error { Code = "NOT_FOUND", Message = "AccountReceivable not found", Language = "EN" });

            // Register payment record if there is an associated sale
            if (ar.SaleId.HasValue)
            {
                var payment = new Onion.Domain.Payment
                {
                    SaleId = ar.SaleId.Value,
                    Amount = req.Amount,
                    PaymentMethod = Onion.Domain.PaymentMethod.OTHER,
                    Reference = req.Reference
                };
                await _uow.Payments.AddAsync(payment);
            }

            // Update account receivable totals
            await _service.RegisterPaymentAsync(id, req.Amount);

            // Record cash movement when cashRegister provided and method is CASH
            if (req.Method != null && req.Method.Equals("CASH", StringComparison.OrdinalIgnoreCase) && req.CashRegisterId.HasValue)
            {
                var cm = new Onion.Domain.CashMovement
                {
                    CashRegisterId = req.CashRegisterId.Value,
                    Amount = req.Amount,
                    Description = $"Payment for AR #{ar.Id}",
                    CompanyId = ar.CompanyId,
                    CreateBy = "system"
                };
                await _uow.CashMovements.AddAsync(cm);
                await _uow.SaveChangesAsync();
            }

            return NoContent();
        }
    }

    public record PaymentRequest(decimal Amount, string? Method = null, string? Reference = null, string? Notes = null, DateTime? Date = null, int? CashRegisterId = null);

    public record CreateReceivableRequest(int CompanyId, int? CustomerId, int? SaleId, decimal TotalAmount, decimal PaidAmount = 0m, DateTime? DueDate = null, PaymentPlanRequest? Plan = null,
        System.Collections.Generic.IEnumerable<Onion.BussinesLogic.Dtos.SaleDetailDto>? Details = null, PaymentRequest? Payment = null);

    public record PaymentPlanRequest(decimal InstallmentAmount, int TotalInstallments, string Frequency, DateTime? StartsAt = null);
}

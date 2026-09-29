using Microsoft.AspNetCore.Mvc;
using Onion.Domain;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;
using Onion.BussinesLogic.Dtos;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IGlobalizationService _globalizationService;

        public PaymentController(IUnitOfWork uow, IGlobalizationService globalizationService)
        {
            _uow = uow;
            _globalizationService = globalizationService;
        }

        [HttpPost("unified")]
        public async Task<IActionResult> RegisterUnified([FromBody] UnifiedPaymentRequest request)
        {
            if (request.Amount <= 0) return BadRequest(new { code = "INVALID_AMOUNT", message = "Amount must be greater than zero" });
            if (request.SaleId is null && request.AccountReceivableId is null && request.AccountPayableId is null)
                return BadRequest(new { code = "PAYMENT_TARGET_REQUIRED", message = "A sale, receivable, or payable target is required" });

            var method = Enum.TryParse<PaymentMethod>(request.Method, true, out var parsed) ? parsed : PaymentMethod.OTHER;
            var companyId = request.CompanyId;
            var payment = new Payment
            {
                SaleId = request.SaleId,
                AccountReceivableId = request.AccountReceivableId,
                AccountPayableId = request.AccountPayableId,
                CompanyId = companyId,
                Amount = request.Amount,
                PaymentMethod = method,
                Reference = request.Reference
            };
            await _uow.Payments.AddAsync(payment);

            if (request.AccountPayableId.HasValue)
            {
                var payable = await _uow.AccountPayables.GetByIdAsync(request.AccountPayableId.Value);
                if (payable == null) return NotFound(new { code = "PAYABLE_NOT_FOUND" });
                payable.PaidAmount += request.Amount;
                payable.Status = payable.PaidAmount >= payable.TotalAmount ? Onion.Domain.Finance.PayableStatus.Paid : Onion.Domain.Finance.PayableStatus.Open;
                _uow.AccountPayables.Update(payable);
            }

            if (request.CashRegisterId.HasValue && method == PaymentMethod.CASH)
            {
                await _uow.CashMovements.AddAsync(new CashMovement
                {
                    CashRegisterId = request.CashRegisterId.Value,
                    Amount = request.AccountPayableId.HasValue ? -request.Amount : request.Amount,
                    CompanyId = companyId,
                    Description = request.AccountPayableId.HasValue ? "Supplier payment" : "Unified payment"
                });
            }

            await _uow.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = payment.Id }, payment);
        }

        public sealed record UnifiedPaymentRequest(decimal Amount, string Method, int CompanyId, int? SaleId = null, int? AccountReceivableId = null, int? AccountPayableId = null, int? CashRegisterId = null, string? Reference = null);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                if (pageNumber.HasValue)
                {
                    var paged = await _uow.Payments.GetPagedAsync(pageNumber.Value, pageSize ?? 10);
                    return Ok(paged);
                }

                var items = await _uow.Payments.ListAsync();
                return Ok(items);
            }
            catch (System.Exception)
            {
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.UnknownException) },
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _uow.Payments.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Payment payment)
        {
            await _uow.Payments.AddAsync(payment);
            await _uow.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = payment.Id }, payment);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Payment payment)
        {
            _uow.Payments.Update(payment);
            await _uow.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _uow.Payments.GetByIdAsync(id);
            if (existing == null) return NotFound();
            _uow.Payments.Remove(existing);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}

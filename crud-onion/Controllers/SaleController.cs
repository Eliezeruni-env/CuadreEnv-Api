using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _service;
        private readonly IGlobalizationService _globalizationService;

        public SaleController(ISaleService service, IGlobalizationService globalizationService)
        {
            _service = service;
            _globalizationService = globalizationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            if (!items.Any())
            {
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.SalesNotFound) },
                    StatusCode = System.Net.HttpStatusCode.BadRequest
                };
            }
            try
            {
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
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Onion.BussinesLogic.Dtos.SaleRequestDto dto)
        {
            // Map DTO -> domain Sale
            var sale = new Sale
            {
                CustomerId = dto.CustomerId,
                Total = dto.Total,
                PaidAmount = dto.PaidAmount,
                CashRegisterId = dto.CashRegisterId,
                DueDate = dto.DueDate,
            };

            foreach (var d in dto.Details)
            {
                sale.Details.Add(new SaleDetail { ProductId = d.ProductId, Quantity = d.Quantity, UnitPrice = d.UnitPrice });
            }

            var created = await _service.CreateAsync(sale);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Sale sale)
        {
            await _service.UpdateAsync(sale);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/payments")]
        public async Task<IActionResult> AddPayment(int id, [FromBody] Onion.BussinesLogic.Dtos.PaymentDto dto)
        {
            var payment = new Payment
            {
                SaleId = id,
                Amount = dto.Amount,
                Reference = dto.Reference
            };

            await _service.AddPaymentAsync(id, payment);
            return NoContent();
        }
    }
}

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
    public class PurchaseController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IGlobalizationService _globalizationService;

        public PurchaseController(IUnitOfWork uow, IGlobalizationService globalizationService)
        {
            _uow = uow;
            _globalizationService = globalizationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var items = await _uow.Purchases.ListAsync();
                // Return 200 with array (possibly empty) rather than treating empty as an error
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
            var item = await _uow.Purchases.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Purchase purchase)
        {
            await _uow.Purchases.AddAsync(purchase);
            await _uow.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = purchase.Id }, purchase);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Purchase purchase)
        {
            _uow.Purchases.Update(purchase);
            await _uow.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _uow.Purchases.GetByIdAsync(id);
            if (existing == null) return NotFound();
            _uow.Purchases.Remove(existing);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}

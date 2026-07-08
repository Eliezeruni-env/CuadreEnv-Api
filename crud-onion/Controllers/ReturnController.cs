using Microsoft.AspNetCore.Mvc;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ReturnController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IGlobalizationService _globalizationService;

        public ReturnController(IUnitOfWork uow, IGlobalizationService globalizationService)
        {
            _uow = uow;
            _globalizationService = globalizationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _uow.Returns.ListAsync();
            if (!items.Any())
            {
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.ReturnsNotFound) },
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
            var item = await _uow.Returns.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Return @return)
        {
            await _uow.Returns.AddAsync(@return);
            await _uow.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = @return.Id }, @return);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Return @return)
        {
            _uow.Returns.Update(@return);
            await _uow.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _uow.Returns.GetByIdAsync(id);
            if (existing == null) return NotFound();
            _uow.Returns.Remove(existing);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;
using Onion.Common.Services;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [Route("customers")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;
        private readonly IGlobalizationService _globalizationService;
        private readonly ICurrentUserService _currentUser;

        public CustomerController(ICustomerService service, IGlobalizationService globalizationService, ICurrentUserService currentUser)
        {
            _service = service;
            _globalizationService = globalizationService;
            _currentUser = currentUser;
        }

        // POST /customer/minimal
        // Create a minimal customer from receivable modal (Name, Identification, Phone, CompanyId)
        [HttpPost("minimal")]
        public async Task<IActionResult> CreateMinimal([FromBody] MinimalCustomerRequest req)
        {
            if (req == null) return BadRequest();
            var companyId = _currentUser.CompanyId;
            if (companyId is not > 0) return BadRequest(new { code = "COMPANY_REQUIRED", message = "Authenticated company is required." });
            if (req.CompanyId is > 0 && req.CompanyId != companyId)
                return Forbid();
            var c = new Customer
            {
                Name = req.Name,
                Identification = req.Identification,
                Phone = req.Phone,
                CompanyId = companyId.Value
            };
            var created = await _service.CreateAsync(c);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize, search);
            // Return paged result even if Items is empty. Empty pages are normal for new companies.
            return Ok(result);
        }

        [HttpGet("corporate")]
        public async Task<IActionResult> GetCorporate([FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var all = await _service.GetAllAsync();
            var query = all.Where(c => c.IsCorporate || c.CreditLimit > 0);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(c => (c.Name != null && c.Name.ToLowerInvariant().Contains(s)) ||
                                         (c.Identification != null && c.Identification.ToLowerInvariant().Contains(s)) ||
                                         (c.ContactPerson != null && c.ContactPerson.ToLowerInvariant().Contains(s)));
            }
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("TODOS", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => string.Equals(c.CorporateStatus, status, StringComparison.OrdinalIgnoreCase));
            }

            var result = query.Select(c => new
            {
                id = c.Id,
                companyName = c.Name,
                rnc = c.Identification ?? "",
                contactPerson = c.ContactPerson ?? c.Name,
                phone = c.Phone ?? "",
                email = c.Email ?? "",
                creditLimit = c.CreditLimit,
                creditDays = c.CreditDays > 0 ? c.CreditDays : 30,
                currentBalance = c.CurrentDebt,
                availableCredit = Math.Max(0, c.CreditLimit - c.CurrentDebt),
                activeOrdersCount = 0,
                status = string.IsNullOrWhiteSpace(c.CorporateStatus) ? "Activo" : c.CorporateStatus
            }).ToList();

            return Ok(new { success = true, data = result });
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] int days = 30)
        {
            var items = await _service.GetActiveCustomersAsync(days);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Customer customer)
        {
            var created = await _service.CreateAsync(customer);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Customer customer)
        {
            await _service.UpdateAsync(customer);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

    public record MinimalCustomerRequest(string Name, string? Identification = null, string? Phone = null, int? CompanyId = null);
    }
}

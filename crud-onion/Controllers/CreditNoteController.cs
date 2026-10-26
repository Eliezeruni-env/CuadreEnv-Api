using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;

namespace Onion.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class CreditNoteController : ControllerBase
    {
        private readonly ICreditNoteService _svc;

        public CreditNoteController(ICreditNoteService svc)
        {
            _svc = svc;
        }

        private int GetCurrentCompanyId()
        {
            var claim = User.FindFirst("CompanyId") ?? User.FindFirst("companyId") ?? User.FindFirst("tenantId");
            if (claim == null || !int.TryParse(claim.Value, out var companyId) || companyId <= 0)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "TENANT_REQUIRED", Message = "Company context is required", Language = "ES" });
            return companyId;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCreditNoteRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var companyId = GetCurrentCompanyId();
            var dto = await _svc.CreateAsync(companyId, request);
            return Ok(Onion.Common.Models.ApiResponse<CreditNoteDto>.Ok(dto, "Credit note created"));
        }
    }
}

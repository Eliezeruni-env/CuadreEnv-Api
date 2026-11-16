using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;

namespace Onion.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Route("credit-notes")]
    [Route("credit-note")]
    [Authorize]
    public class CreditNoteController : ControllerBase
    {
        private readonly ICreditNoteService _svc;
        private readonly Onion.Common.Services.ICurrentUserService _currentUser;

        public CreditNoteController(ICreditNoteService svc, Onion.Common.Services.ICurrentUserService currentUser)
        {
            _svc = svc;
            _currentUser = currentUser;
        }

        private int GetCurrentCompanyId()
        {
            if (_currentUser.CompanyId is not > 0)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "TENANT_REQUIRED", Message = "Company context is required", Language = "ES" });
            return _currentUser.CompanyId.Value;
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

using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("my")]
        public async Task<IActionResult> MySubscription()
        {
            var companyClaim = User?.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(companyClaim, out var companyId)) return BadRequest("CompanyId claim missing");
            var sub = await _subscriptionService.GetCompanySubscriptionAsync(companyId);
            if (sub == null) return NotFound();
            return Ok(sub);
        }
    }
}

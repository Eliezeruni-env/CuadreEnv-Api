using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly Onion.Common.Authorization.IAuthorizationService _auth;

        public SubscriptionController(ISubscriptionService subscriptionService, Onion.Common.Authorization.IAuthorizationService auth)
        {
            _subscriptionService = subscriptionService;
            _auth = auth;
        }

        [HttpGet("my")]
        public async Task<IActionResult> MySubscription()
        {
            if (!_auth.TryGetCompanyId(User, out var companyId)) return BadRequest("CompanyId claim missing");
            var sub = await _subscriptionService.GetCompanySubscriptionAsync(companyId);
            if (sub == null) return NotFound();
            return Ok(sub);
        }
    }
}

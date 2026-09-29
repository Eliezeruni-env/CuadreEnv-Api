using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Onion.Common.Authorization;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Billing;
using Xunit;
using System.Threading.Tasks;

namespace Onion.Tests
{
    public class SubscriptionControllerTests
    {
        private ClaimsPrincipal CreateUser(int? companyId = null)
        {
            var claims = new System.Collections.Generic.List<Claim>();
            if (companyId.HasValue) claims.Add(new Claim("CompanyId", companyId.Value.ToString()));
            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }

        private class FakeSubscriptionService : ISubscriptionService
        {
            public Task<CompanySubscription?> GetCompanySubscriptionAsync(int companyId)
            {
                if (companyId == 5)
                    return Task.FromResult<CompanySubscription?>(new CompanySubscription { Id = 1, CompanyId = 5, StartDate = System.DateTime.UtcNow, Status = SubscriptionStatus.Active });
                return Task.FromResult<CompanySubscription?>(null);
            }

            public Task<bool> CanCreateUserAsync(int companyId) => Task.FromResult(true);
            public Task<bool> CanCreateWarehouseAsync(int companyId) => Task.FromResult(true);
            public Task<bool> CanCreateProductAsync(int companyId) => Task.FromResult(true);
        }

        [Fact]
        public async Task MySubscription_BadRequest_When_CompanyId_Missing()
        {
            var svc = new FakeSubscriptionService();
            var ctrl = new Onion.Controllers.SubscriptionController(svc, new AuthorizationService());
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

            var result = await ctrl.MySubscription();
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task MySubscription_Returns_Ok_When_Present()
        {
            var svc = new FakeSubscriptionService();
            var ctrl = new Onion.Controllers.SubscriptionController(svc, new AuthorizationService());
            var user = CreateUser(5);
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var result = await ctrl.MySubscription();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }
    }
}

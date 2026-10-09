using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;
using Xunit;
using System.Threading.Tasks;

namespace Onion.Tests
{
    public class WarehouseControllerTests
    {
        private ClaimsPrincipal CreateUser(string? role = null, int? companyId = null)
        {
            var claims = new System.Collections.Generic.List<Claim>();
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }
            if (companyId.HasValue)
            {
                claims.Add(new Claim("CompanyId", companyId.Value.ToString()));
            }
            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }

        private class FakeWarehouseService : IWarehouseService
        {
            public int? RequestedCompanyId { get; private set; }

            public Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, int companyId)
            {
            return Task.FromResult(new WarehouseDto(123, dto.Name, companyId));
            }

            public Task AddStockAsync(MovementRequestDto req, string performedBy) => Task.CompletedTask;
            public Task RemoveStockAsync(MovementRequestDto req, string performedBy) => Task.CompletedTask;
            public Task TransferStockAsync(TransferRequestDto req, string performedBy) => Task.CompletedTask;
            public Task<System.Collections.Generic.IEnumerable<WarehouseDto>> GetAllAsync(int companyId)
            {
                RequestedCompanyId = companyId;
                return Task.FromResult<System.Collections.Generic.IEnumerable<WarehouseDto>>(new[] { new WarehouseDto(1, "W1", companyId) });
            }
            public Task<System.Collections.Generic.IEnumerable<ProductDto>> GetLowStockAsync() => Task.FromResult<System.Collections.Generic.IEnumerable<ProductDto>>(new ProductDto[0]);
            public Task<System.Collections.Generic.IEnumerable<MovementDto>> GetMovementHistoryAsync(int? productId = null, int? warehouseId = null, System.DateTime? from = null, System.DateTime? to = null, string? type = null) => Task.FromResult<System.Collections.Generic.IEnumerable<MovementDto>>(new MovementDto[0]);
            public Task<System.Collections.Generic.IEnumerable<Onion.Domain.Inventory.InventoryMovement>> GetInventoryMovementsAsync(int? productId = null, int? warehouseId = null, System.DateTime? from = null, System.DateTime? to = null, string? type = null) => Task.FromResult<System.Collections.Generic.IEnumerable<Onion.Domain.Inventory.InventoryMovement>>(new Onion.Domain.Inventory.InventoryMovement[0]);
            public Task<Onion.Common.Models.Pagination.PagedList<WarehouseDto>> GetPagedAsync(int pageNumber, int pageSize, int companyId) => throw new System.NotImplementedException();
            public Task<Onion.Common.Models.Pagination.PagedList<MovementDto>> GetMovementHistoryPagedAsync(int? productId = null, int? warehouseId = null, System.DateTime? from = null, System.DateTime? to = null, string? type = null, int pageNumber = 1, int pageSize = 10) => throw new System.NotImplementedException();
            public Task<Onion.Common.Models.Pagination.PagedList<Onion.Domain.Inventory.InventoryMovement>> GetInventoryMovementsPagedAsync(int? productId = null, int? warehouseId = null, System.DateTime? from = null, System.DateTime? to = null, string? type = null, int pageNumber = 1, int pageSize = 10) => throw new System.NotImplementedException();
        }

        [Fact]
        public async Task Create_Returns_BadRequest_When_CompanyId_Missing()
        {
            var svc = new FakeWarehouseService();
            var auth = new AuthorizationService();
            var ctrl = new Onion.Controllers.WarehouseController(svc, null!, auth);
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

            var result = await ctrl.Create(new CreateWarehouseDto("X"));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_Returns_Created_When_CompanyId_Present()
        {
            var svc = new FakeWarehouseService();
            var auth = new AuthorizationService();
            var ctrl = new Onion.Controllers.WarehouseController(svc, null!, auth);
            var user = CreateUser("Employee", 7);
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var result = await ctrl.Create(new CreateWarehouseDto("MyW"));
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.NotNull(created.Value);
        }

        [Fact]
        public async Task GetAll_Uses_Authenticated_CompanyId()
        {
            var svc = new FakeWarehouseService();
            var auth = new AuthorizationService();
            var ctrl = new Onion.Controllers.WarehouseController(svc, null!, auth);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUser("Employee", 7) }
            };

            var result = await ctrl.GetAll();

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(7, svc.RequestedCompanyId);
        }

        [Fact]
        public async Task AddStock_BadRequest_When_CompanyId_Missing()
        {
            var svc = new FakeWarehouseService();
            var auth = new AuthorizationService();
            var ctrl = new Onion.Controllers.WarehouseController(svc, null!, auth);
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

            var result = await ctrl.AddStock(new MovementRequestDto(0,0,0));
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}

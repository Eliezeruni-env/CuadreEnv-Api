using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;
using Onion.Common.Authorization;
using System;
using System.Threading.Tasks;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [AuthorizeModule("PURCHASES")]
    public class PurchaseController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IGlobalizationService _globalizationService;
        private readonly ICurrentUserService _currentUser;

        public PurchaseController(IUnitOfWork uow, IGlobalizationService globalizationService, ICurrentUserService currentUser)
        {
            _uow = uow;
            _globalizationService = globalizationService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25)
        {
            try
            {
                var paged = await _uow.Purchases.GetPagedAsync(Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, 100));
                return Ok(new
                {
                    data = paged.Items,
                    pageNumber = Math.Max(1, pageNumber),
                    pageSize = paged.PageSize,
                    totalCount = paged.TotalItemCount,
                    totalPages = paged.PageCount
                });
            }
            catch (Exception)
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
            var item = await _uow.Purchases.GetByIdWithDetailsAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Purchase purchase)
        {
            if (purchase == null) return BadRequest(new { message = "Purchase payload is required" });

            var companyId = _currentUser.CompanyId;
            if (!companyId.HasValue || companyId.Value <= 0) return Forbid();
            if (purchase.CompanyId > 0 && purchase.CompanyId != companyId.Value) return Forbid();
            purchase.CompanyId = companyId.Value;

            purchase.CreationDate = DateTime.UtcNow;
            purchase.Active = true;
            if (string.IsNullOrEmpty(purchase.CreateBy))
                purchase.CreateBy = User.Identity?.Name ?? "system";
            if (string.IsNullOrEmpty(purchase.ModifiedBy))
                purchase.ModifiedBy = purchase.CreateBy;

            if (purchase.Details != null)
            {
                foreach (var detail in purchase.Details)
                {
                    detail.CreationDate = DateTime.UtcNow;
                    detail.Active = true;
                    if (string.IsNullOrEmpty(detail.CreateBy))
                        detail.CreateBy = purchase.CreateBy;
                    if (string.IsNullOrEmpty(detail.ModifiedBy))
                        detail.ModifiedBy = purchase.ModifiedBy;
                    if (detail.Cost == 0 && detail.CostPrice > 0)
                        detail.Cost = detail.CostPrice;
                }
            }

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

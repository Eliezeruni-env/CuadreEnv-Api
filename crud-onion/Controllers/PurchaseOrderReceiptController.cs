using Microsoft.AspNetCore.Mvc;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.BussinesLogic.Dtos;
using Onion.Domain.Purchases;
using Onion.Domain.ManageRequests;
using System.Text.Json;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PurchaseOrderReceiptController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly Onion.DataAccess.OnionDbContext _db;
        private readonly IGlobalizationService _globalizationService;

        public PurchaseOrderReceiptController(IUnitOfWork uow, IGlobalizationService globalizationService, Onion.DataAccess.OnionDbContext db)
        {
            _uow = uow;
            _globalizationService = globalizationService;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _uow.PurchaseOrderReceipts.ListAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _uow.PurchaseOrderReceipts.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("exists/{purchaseId}")]
        public async Task<IActionResult> Exists(int purchaseId)
        {
            var purchase = await _uow.Purchases.GetByIdAsync(purchaseId);
            if (purchase == null) return NotFound();

            // Completed if all details received
            var allReceived = purchase.Details != null && purchase.Details.All(d => d.QuantityReceived >= d.Quantity);
            if (allReceived) return Ok(true);

            // Or there's a pending manage request for this purchase
            var pending = (await _uow.ManageRequests.FindAsync(m => m.PayloadJson.Contains($"\"PurchaseId\":{purchaseId}"))).Any();
            return Ok(pending);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PurchaseOrderReceiptDto dto)
        {
            if (dto == null) return BadRequest();

            // Check purchase exists
            var purchase = await _uow.Purchases.GetByIdAsync(dto.PurchaseId);
            if (purchase == null) return NotFound();

            // Detect discrepancies: received > ordered
            var discrepancy = false;
            foreach (var line in dto.Details)
            {
                var pd = purchase.Details.FirstOrDefault(d => d.ProductId == line.ProductId);
                if (pd == null) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_LINE", Message = "Purchase does not contain product", Language = "EN" });
                if (line.QuantityReceived > pd.Quantity) discrepancy = true;
            }

            if (discrepancy)
            {
                // Create ManageRequest instead of applying stock
                var mr = new Onion.Domain.ManageRequests.ManageRequest
                {
                    Type = ManageRequestType.PurchaseReceipt,
                    PayloadJson = JsonSerializer.Serialize(dto),
                    Status = ManageRequestStatus.Pending,
                    CompanyId = purchase.CompanyId,
                    CreatedBy = dto.SupplierId.ToString()
                };
                await _db.Set<Onion.Domain.ManageRequests.ManageRequest>().AddAsync(mr);
                await _uow.SaveChangesAsync();
                return Accepted(new { requestId = mr.Id });
            }

            // Apply directly: transactionally add receipt + update inventory + update purchase details
            using var tx = await _uow.BeginTransactionAsync();
            try
            {
                var receipt = new PurchaseOrderReceipt
                {
                    PurchaseId = dto.PurchaseId,
                    WarehouseId = dto.WarehouseId,
                    SupplierId = dto.SupplierId,
                    ReceiptDate = dto.ReceiptDate,
                    CompanyId = purchase.CompanyId,
                    CreatedBy = dto.SupplierId.ToString()
                };

                foreach (var line in dto.Details)
                {
                    receipt.Details.Add(new PurchaseOrderReceiptDetail
                    {
                        ProductId = line.ProductId,
                        QuantityReceived = line.QuantityReceived,
                        UnitCost = line.UnitCost,
                        WarehouseId = line.WarehouseId,
                        CompanyId = purchase.CompanyId
                    });

                    // update or create inventory record
                    var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(line.ProductId, line.WarehouseId);
                    if (inv == null)
                    {
                        inv = new Onion.Domain.Warehouses.Inventory { ProductId = line.ProductId, WarehouseId = line.WarehouseId, Quantity = line.QuantityReceived, CompanyId = purchase.CompanyId };
                        await _uow.Inventories.AddAsync(inv);
                    }
                    else
                    {
                        inv.Quantity += line.QuantityReceived;
                        _uow.Inventories.Update(inv);
                    }

                    // update purchase detail received qty
                    var pd = purchase.Details.First(d => d.ProductId == line.ProductId);
                    pd.QuantityReceived += line.QuantityReceived;
                }

                await _uow.PurchaseOrderReceipts.AddAsync(receipt);
                // if all lines complete, mark purchase as completed
                if (purchase.Details.All(d => d.QuantityReceived >= d.Quantity))
                {
                    purchase.Total = purchase.Total; // keep
                    // optionally set status via a property; Purchase lacks status field, ignore for now
                }

                await _uow.SaveChangesAsync();
                await tx.CommitAsync();
                return CreatedAtAction(nameof(Post), new { id = receipt.Id }, receipt);
            }
            catch (System.Exception)
            {
                await tx.RollbackAsync();
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(Onion.Common.Enums.ErrorCodes.UnknownException) },
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
            }
        }
    }
}

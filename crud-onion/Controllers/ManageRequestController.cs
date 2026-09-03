using Microsoft.AspNetCore.Mvc;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using System.Text.Json;
using Onion.Domain.ManageRequests;
using Onion.BussinesLogic.Dtos;
using Onion.Domain.Purchases;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ManageRequestController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IGlobalizationService _globalizationService;

        public ManageRequestController(IUnitOfWork uow, IGlobalizationService globalizationService)
        {
            _uow = uow;
            _globalizationService = globalizationService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ManageRequest req)
        {
            if (req == null) return BadRequest();
            req.Status = ManageRequestStatus.Pending;
            await _uow.ManageRequests.AddAsync(req);
            await _uow.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = req.Id }, req);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _uow.ManageRequests.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromQuery] string approvedBy = "system")
        {
            var mr = await _uow.ManageRequests.GetByIdAsync(id);
            if (mr == null) return NotFound();
            if (mr.Status != ManageRequestStatus.Pending) return BadRequest("Request not pending");

            // Only PurchaseReceipt type implemented here
            if (mr.Type == ManageRequestType.PurchaseReceipt)
            {
                var dto = JsonSerializer.Deserialize<PurchaseOrderReceiptDto>(mr.PayloadJson);
                if (dto == null) return BadRequest("Invalid payload");

                using var tx = await _uow.BeginTransactionAsync();
                try
                {
                    // apply receipt as in PurchaseOrderReceiptController
                    var purchase = await _uow.Purchases.GetByIdAsync(dto.PurchaseId);
                    if (purchase == null) return NotFound();

                    var receipt = new PurchaseOrderReceipt
                    {
                        PurchaseId = dto.PurchaseId,
                        WarehouseId = dto.WarehouseId,
                        SupplierId = dto.SupplierId,
                        ReceiptDate = dto.ReceiptDate,
                        CompanyId = purchase.CompanyId,
                        CreatedBy = approvedBy
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

                        var pd = purchase.Details.First(d => d.ProductId == line.ProductId);
                        pd.QuantityReceived += line.QuantityReceived;
                    }

                    await _uow.PurchaseOrderReceipts.AddAsync(receipt);
                    mr.Status = ManageRequestStatus.Approved;
                    mr.Timeline.Add(new ManageRequestTimeline { Action = "Approved", User = approvedBy, OldStatus = (int)ManageRequestStatus.Pending, NewStatus = (int)ManageRequestStatus.Approved, Comment = "Approved via API" });
                    _uow.ManageRequests.Update(mr);
                    await _uow.SaveChangesAsync();
                    await tx.CommitAsync();
                    return Ok(receipt);
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

            return BadRequest("ManageRequest type not supported yet");
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] string reason)
        {
            var mr = await _uow.ManageRequests.GetByIdAsync(id);
            if (mr == null) return NotFound();
            if (mr.Status != ManageRequestStatus.Pending) return BadRequest("Request not pending");

            mr.Status = ManageRequestStatus.Rejected;
            mr.Comment = reason;
            mr.Timeline.Add(new ManageRequestTimeline { Action = "Rejected", User = "supervisor", OldStatus = (int)ManageRequestStatus.Pending, NewStatus = (int)ManageRequestStatus.Rejected, Comment = reason });
            _uow.ManageRequests.Update(mr);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}

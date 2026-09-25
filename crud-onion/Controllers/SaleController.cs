using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;
using Onion.Common.Authorization;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [AuthorizeModule("SALES")]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _service;
        private readonly IGlobalizationService _globalizationService;
        private readonly IEmailService _emailService;
        private readonly Onion.DataAccess.OnionDbContext? _dbContext;
        private readonly ILogger<SaleController> _logger;

        public SaleController(
            ISaleService service,
            IGlobalizationService globalizationService,
            IEmailService emailService,
            ILogger<SaleController> logger,
            Onion.DataAccess.OnionDbContext? dbContext = null)
        {
            _service = service;
            _globalizationService = globalizationService;
            _emailService = emailService;
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                if (pageNumber.HasValue)
                {
                    var paged = await _service.GetPagedAsync(pageNumber.Value, pageSize ?? 10);
                    return Ok(paged);
                }

                var items = await _service.GetAllAsync();
                return Ok(items);
            }
            catch (System.Exception)
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
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("{id}/send-email")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> SendEmail(int id, [FromBody] SendInvoiceEmailDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("Email is required."));

            if (!MailAddress.TryCreate(request.Email.Trim(), out _))
                return BadRequest(Onion.Common.Models.ApiResponse<string>.Fail("The email address is invalid."));

            var sale = await _service.GetByIdAsync(id);
            if (sale == null)
                return NotFound(Onion.Common.Models.ApiResponse<string>.Fail("Sale not found."));

            Company? company = null;
            Customer? customer = null;
            var productNames = new Dictionary<int, string>();

            if (_dbContext != null)
            {
                try
                {
                    if (sale.CompanyId > 0)
                    {
                        company = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                            _dbContext.Companies, c => c.Id == sale.CompanyId);
                    }

                    if (sale.CustomerId.HasValue && sale.CustomerId.Value > 0)
                    {
                        customer = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                            _dbContext.Customers, c => c.Id == sale.CustomerId.Value);
                    }

                    if (sale.Details != null && sale.Details.Count > 0)
                    {
                        var productIds = sale.Details.Select(d => d.ProductId).Distinct().ToList();
                        var products = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                            _dbContext.Products.Where(p => productIds.Contains(p.Id)));
                        foreach (var prod in products)
                        {
                            productNames[prod.Id] = prod.Description ?? $"Producto #{prod.Id}";
                        }
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Could not load related entities for invoice email sale {SaleId}.", id);
                }
            }

            var invNumber = !string.IsNullOrWhiteSpace(request.InvoiceNumber)
                ? request.InvoiceNumber.Trim()
                : (!string.IsNullOrWhiteSpace(sale.InvoiceFolio) ? sale.InvoiceFolio : $"VTA-{sale.Id:D6}");

            var compName = company?.Name ?? "CuadreEnv";
            var subject = string.IsNullOrWhiteSpace(request.Subject)
                ? $"Factura {invNumber} - {compName}"
                : request.Subject.Trim();

            var message = InvoiceEmailBuilder.BuildInvoiceHtml(sale, request, company, customer, productNames, invNumber);

            byte[]? attachmentBytes = null;
            string? attachmentName = null;
            if (request.AttachPdf)
            {
                attachmentBytes = InvoiceEmailBuilder.BuildInvoicePdf(sale, request, company, customer, productNames, invNumber);
                attachmentName = $"factura-{invNumber}.pdf";
            }

            try
            {
                await _emailService.SendEmailAsync(
                    request.Email.Trim(),
                    subject,
                    message,
                    isHtml: true,
                    attachmentBytes,
                    attachmentName);

                return Ok(Onion.Common.Models.ApiResponse<string>.Ok("Correo enviado con éxito"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "SMTP configuration is invalid while sending invoice email for sale {SaleId}.", id);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        errorCode = "SMTP_NOT_CONFIGURED",
                        message = "El servicio de correo no está configurado correctamente. Contacte al administrador."
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send invoice email for sale {SaleId}.", id);
                return StatusCode(StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        errorCode = "SMTP_DELIVERY_FAILED",
                        message = "El servidor de correo rechazó o no pudo procesar el envío. Intente nuevamente más tarde."
                    });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Onion.BussinesLogic.Dtos.SaleRequestDto dto)
        {
            var idempotencyKey = Request.Headers.TryGetValue("X-Idempotency-Key", out var ik) ? ik.ToString() : null;
            if (!string.IsNullOrWhiteSpace(idempotencyKey) && _dbContext != null)
            {
                var existing = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    _dbContext.Sales, s => s.IdempotencyKey == idempotencyKey && !s.IsDeleted);
                if (existing != null)
                {
                    return Ok(existing);
                }
            }
            // Map DTO -> domain Sale
            var sale = new Sale
            {
                CustomerId = dto.CustomerId,
                Total = dto.Total,
                PaidAmount = dto.PaidAmount,
                CashRegisterId = dto.CashRegisterId,
                DueDate = dto.DueDate,
                IdempotencyKey = idempotencyKey,
            };

            foreach (var d in dto.Details)
            {
                sale.Details.Add(new SaleDetail { ProductId = d.ProductId, Quantity = d.Quantity, UnitPrice = d.UnitPrice });
            }

            var created = await _service.CreateAsync(sale);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Sale sale)
        {
            await _service.UpdateAsync(sale);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/payments")]
        public async Task<IActionResult> AddPayment(int id, [FromBody] Onion.BussinesLogic.Dtos.PaymentDto dto)
        {
            var payment = new Payment
            {
                SaleId = id,
                Amount = dto.Amount,
                Reference = dto.Reference
            };

            await _service.AddPaymentAsync(id, payment, dto.Method, dto.CashRegisterId);
            return NoContent();
        }

        // Cancel a sale (Manager or Admin)
        [HttpPost("{id}/cancel")]
        [Onion.Common.Authorization.RequireRole(Onion.Common.Authorization.Roles.Admin, Onion.Common.Authorization.Roles.Manager)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelRequest req)
        {
            await _service.CancelAsync(id, req.Reason ?? string.Empty);
            return NoContent();
        }
    }

    public record CancelRequest(string? Reason);
    public record SendInvoiceEmailItemDto(string ProductName, string? ProductCode, decimal Quantity, decimal UnitPrice, decimal Total);

    public record SendInvoiceEmailDto(
        string Email,
        string? Subject,
        string? Message,
        bool AttachPdf,
        string? InvoiceHtml = null,
        string? InvoiceNumber = null,
        string? CustomerName = null,
        string? CustomerRnc = null,
        string? CashierName = null,
        string? CashRegisterName = null,
        string? PaymentMethod = null,
        List<SendInvoiceEmailItemDto>? Items = null,
        decimal? Subtotal = null,
        decimal? Discount = null,
        decimal? Itbis = null,
        decimal? Total = null,
        string? Notes = null);

    internal static class InvoiceEmailBuilder
    {
        public static string BuildInvoiceHtml(
            Sale sale,
            SendInvoiceEmailDto request,
            Company? company,
            Customer? customer,
            Dictionary<int, string> productNames,
            string invoiceNumber)
        {
            if (!string.IsNullOrWhiteSpace(request.InvoiceHtml))
            {
                var personalMessage = !string.IsNullOrWhiteSpace(request.Message)
                    ? $"""
                      <div style="max-width: 650px; margin: 0 auto 20px auto; padding: 16px 20px; background-color: #f8fafc; border-left: 4px solid #2563eb; border-radius: 6px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; font-size: 14px; color: #334155; line-height: 1.6;">
                        {WebUtility.HtmlEncode(request.Message).Replace("\n", "<br/>")}
                      </div>
                      """
                    : string.Empty;

                return $"""
                <!doctype html>
                <html>
                <head><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/></head>
                <body style="margin: 0; padding: 24px 12px; background-color: #f1f5f9;">
                  {personalMessage}
                  {request.InvoiceHtml}
                </body>
                </html>
                """;
            }

            var compName = company?.Name ?? "CuadreEnv, SRL";
            var compCommercial = company?.Settings?.CommercialName ?? "Soluciones de Facturación & Control";
            var compRnc = company?.Rnc ?? "1-01-00000-0";
            var compAddress = company?.Address ?? "Santo Domingo, República Dominicana";
            var compPhone = company?.Phone ?? "(809) 555-0199";

            var custName = !string.IsNullOrWhiteSpace(request.CustomerName)
                ? request.CustomerName
                : (customer?.Name ?? "Consumidor final");
            var custRnc = !string.IsNullOrWhiteSpace(request.CustomerRnc)
                ? request.CustomerRnc
                : (customer?.Identification ?? "000-0000000-0");

            var cashier = request.CashierName ?? "Admin";
            var cashRegister = request.CashRegisterName ?? "Caja Principal";
            var paymentMethod = request.PaymentMethod ?? "Efectivo";
            var notes = request.Notes ?? sale.Notes ?? string.Empty;

            var items = new List<(string Name, string Code, decimal Qty, decimal Price, decimal LineTotal)>();
            if (request.Items != null && request.Items.Count > 0)
            {
                foreach (var itm in request.Items)
                {
                    items.Add((itm.ProductName, itm.ProductCode ?? "-", itm.Quantity, itm.UnitPrice, itm.Total));
                }
            }
            else if (sale.Details != null && sale.Details.Count > 0)
            {
                foreach (var d in sale.Details)
                {
                    var name = productNames.TryGetValue(d.ProductId, out var pName) ? pName : $"Producto #{d.ProductId}";
                    items.Add((name, $"PROD-{d.ProductId}", d.Quantity, d.UnitPrice, d.Quantity * d.UnitPrice));
                }
            }
            else
            {
                items.Add(("Venta / Factura general", "GEN-01", 1, sale.Total, sale.Total));
            }

            var subtotal = request.Subtotal ?? (sale.SubTotal > 0 ? sale.SubTotal : sale.Total);
            var itbis = request.Itbis ?? (sale.Tax > 0 ? sale.Tax : (sale.Total * 0.18m));
            var total = request.Total ?? sale.Total;
            var discount = request.Discount ?? 0m;

            var rowsHtml = new StringBuilder();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var lineItbis = item.LineTotal * 0.18m;
                rowsHtml.Append($"""
                <tr style="border-bottom: 1px solid #e2e8f0; font-size: 13px;">
                  <td style="padding: 10px 8px; color: #64748b; font-family: monospace;">{i + 1}</td>
                  <td style="padding: 10px 8px; font-weight: 600; color: #1e293b;">{WebUtility.HtmlEncode(item.Name)}</td>
                  <td style="padding: 10px 8px; color: #64748b; font-family: monospace; font-size: 12px;">{WebUtility.HtmlEncode(item.Code)}</td>
                  <td style="padding: 10px 8px; text-align: right; font-family: monospace;">RD$ {item.Price:N2}</td>
                  <td style="padding: 10px 8px; text-align: center; font-family: monospace;">{item.Qty:G29}</td>
                  <td style="padding: 10px 8px; text-align: right; color: #64748b; font-family: monospace;">RD$ {lineItbis:N2}</td>
                  <td style="padding: 10px 8px; text-align: right; font-weight: 700; color: #1e293b; font-family: monospace;">RD$ {item.LineTotal:N2}</td>
                </tr>
                """);
            }

            var personalMessageBanner = !string.IsNullOrWhiteSpace(request.Message)
                ? $"""
                  <div style="max-width: 650px; margin: 0 auto 20px auto; padding: 16px 20px; background-color: #f8fafc; border-left: 4px solid #2563eb; border-radius: 6px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; font-size: 14px; color: #334155; line-height: 1.6;">
                    {WebUtility.HtmlEncode(request.Message).Replace("\n", "<br/>")}
                  </div>
                  """
                : string.Empty;

            return $"""
            <!doctype html>
            <html>
            <head><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/></head>
            <body style="margin: 0; padding: 24px 12px; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;">
              {personalMessageBanner}
              <div style="max-width: 650px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);">
                <div style="background-color: #ffffff; padding: 24px 28px; border-bottom: 2px solid #2563eb;">
                  <table style="width: 100%; border-collapse: collapse;">
                    <tr>
                      <td style="vertical-align: middle;">
                        <h2 style="margin: 0; color: #0f172a; font-size: 22px; font-weight: 800; letter-spacing: -0.5px;">{WebUtility.HtmlEncode(compName)}</h2>
                        <div style="color: #64748b; font-size: 13px; margin-top: 4px;">{WebUtility.HtmlEncode(compCommercial)}</div>
                      </td>
                      <td style="text-align: right; vertical-align: middle;">
                        <div style="font-size: 12px; font-weight: 700; color: #64748b; letter-spacing: 1px;">FACTURA</div>
                        <div style="font-size: 20px; font-weight: 800; color: #2563eb; font-family: monospace; margin-top: 2px;">No. {WebUtility.HtmlEncode(invoiceNumber)}</div>
                      </td>
                    </tr>
                  </table>
                </div>

                <div style="padding: 18px 28px; background-color: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                  <table style="width: 100%; border-collapse: collapse; font-size: 12px; color: #475569;">
                    <tr>
                      <td style="width: 55%; vertical-align: top; line-height: 1.6;">
                        <strong style="color: #1e293b;">{WebUtility.HtmlEncode(compName)}</strong><br/>
                        RNC: {WebUtility.HtmlEncode(compRnc)}<br/>
                        {WebUtility.HtmlEncode(compAddress)}<br/>
                        Tel: {WebUtility.HtmlEncode(compPhone)}
                      </td>
                      <td style="width: 45%; vertical-align: top; text-align: right; line-height: 1.6;">
                        <span style="color: #1e293b; font-weight: 600;">Fecha:</span> {sale.Date:dd/MM/yyyy, hh:mm tt}<br/>
                        <span style="color: #1e293b; font-weight: 600;">Caja:</span> {WebUtility.HtmlEncode(cashRegister)}<br/>
                        <span style="color: #1e293b; font-weight: 600;">Cajero:</span> {WebUtility.HtmlEncode(cashier)}
                      </td>
                    </tr>
                  </table>
                </div>

                <div style="padding: 16px 28px 8px 28px;">
                  <div style="background-color: #f1f5f9; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px 16px;">
                    <div style="font-size: 11px; font-weight: 700; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">Cliente</div>
                    <div style="font-size: 15px; font-weight: 700; color: #0f172a; margin-top: 2px;">{WebUtility.HtmlEncode(custName)}</div>
                    <div style="font-size: 12px; color: #475569; font-family: monospace; margin-top: 2px;">RNC / Cédula: {WebUtility.HtmlEncode(custRnc)}</div>
                  </div>
                </div>

                <div style="padding: 16px 28px;">
                  <table style="width: 100%; border-collapse: collapse;">
                    <thead>
                      <tr style="background-color: #f8fafc; border-bottom: 2px solid #cbd5e1; font-size: 12px; color: #475569; text-transform: uppercase;">
                        <th style="padding: 8px; text-align: left; width: 25px;">#</th>
                        <th style="padding: 8px; text-align: left;">Producto</th>
                        <th style="padding: 8px; text-align: left;">Código</th>
                        <th style="padding: 8px; text-align: right;">Precio</th>
                        <th style="padding: 8px; text-align: center;">Cant.</th>
                        <th style="padding: 8px; text-align: right;">ITBIS (18%)</th>
                        <th style="padding: 8px; text-align: right;">Total</th>
                      </tr>
                    </thead>
                    <tbody>
                      {rowsHtml}
                    </tbody>
                  </table>
                </div>

                <div style="padding: 0 28px 24px 28px;">
                  <table style="width: 100%; border-collapse: collapse;">
                    <tr>
                      <td style="vertical-align: top; width: 50%; padding-right: 14px;">
                        {(string.IsNullOrWhiteSpace(notes) ? "" : $"""
                        <div style="background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 10px 14px; font-size: 12px;">
                          <strong style="color: #475569; display: block; margin-bottom: 4px;">Observaciones:</strong>
                          <span style="color: #64748b;">{WebUtility.HtmlEncode(notes)}</span>
                        </div>
                        """)}
                        <div style="margin-top: 12px; font-size: 13px; color: #475569;">
                          <strong>Método de pago:</strong> <span style="background: #e0f2fe; color: #0369a1; padding: 3px 8px; border-radius: 4px; font-weight: 600; font-size: 12px;">{WebUtility.HtmlEncode(paymentMethod)}</span>
                        </div>
                      </td>
                      <td style="vertical-align: top; width: 50%; padding-left: 14px;">
                        <div style="background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 14px 18px;">
                          <table style="width: 100%; font-size: 13px; color: #475569;">
                            <tr>
                              <td style="padding-bottom: 6px;">Subtotal</td>
                              <td style="text-align: right; font-family: monospace; font-weight: 600;">RD$ {subtotal:N2}</td>
                            </tr>
                            <tr>
                              <td style="padding-bottom: 6px;">Descuento</td>
                              <td style="text-align: right; font-family: monospace; color: #64748b;">RD$ {discount:N2}</td>
                            </tr>
                            <tr>
                              <td style="padding-bottom: 8px;">ITBIS (18%)</td>
                              <td style="text-align: right; font-family: monospace; color: #64748b;">RD$ {itbis:N2}</td>
                            </tr>
                            <tr style="border-top: 2px solid #e2e8f0; font-size: 16px; font-weight: 800; color: #0f172a;">
                              <td style="padding-top: 8px;">Total</td>
                              <td style="text-align: right; font-family: monospace; color: #2563eb; padding-top: 8px;">RD$ {total:N2}</td>
                            </tr>
                          </table>
                        </div>
                      </td>
                    </tr>
                  </table>
                </div>

                <div style="background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 16px 28px; text-align: center; font-size: 12px; color: #64748b;">
                  <div style="font-weight: 600; color: #1e293b; margin-bottom: 4px;">"¡Gracias por su preferencia!"</div>
                  <div style="font-size: 11px; color: #94a3b8;">Este documento es un comprobante de venta válido emitido electrónicamente.</div>
                </div>
              </div>
            </body>
            </html>
            """;
        }

        public static byte[] BuildInvoicePdf(
            Sale sale,
            SendInvoiceEmailDto request,
            Company? company,
            Customer? customer,
            Dictionary<int, string> productNames,
            string invoiceNumber)
        {
            var compName = company?.Name ?? "CuadreEnv, SRL";
            var compCommercial = company?.Settings?.CommercialName ?? "Soluciones de Facturacion & Control";
            var compRnc = company?.Rnc ?? "1-01-00000-0";
            var compAddress = company?.Address ?? "Santo Domingo, Republica Dominicana";
            var compPhone = company?.Phone ?? "(809) 555-0199";

            var custName = !string.IsNullOrWhiteSpace(request.CustomerName)
                ? request.CustomerName
                : (customer?.Name ?? "Consumidor final");
            var custRnc = !string.IsNullOrWhiteSpace(request.CustomerRnc)
                ? request.CustomerRnc
                : (customer?.Identification ?? "000-0000000-0");

            var cashier = request.CashierName ?? "Admin";
            var cashRegister = request.CashRegisterName ?? "Caja Principal";
            var paymentMethod = request.PaymentMethod ?? "Efectivo";

            var items = new List<(string Name, string Code, decimal Qty, decimal Price, decimal LineTotal)>();
            if (request.Items != null && request.Items.Count > 0)
            {
                foreach (var itm in request.Items)
                {
                    items.Add((itm.ProductName, itm.ProductCode ?? "-", itm.Quantity, itm.UnitPrice, itm.Total));
                }
            }
            else if (sale.Details != null && sale.Details.Count > 0)
            {
                foreach (var d in sale.Details)
                {
                    var name = productNames.TryGetValue(d.ProductId, out var pName) ? pName : $"Producto #{d.ProductId}";
                    items.Add((name, $"PROD-{d.ProductId}", d.Quantity, d.UnitPrice, d.Quantity * d.UnitPrice));
                }
            }
            else
            {
                items.Add(("Venta / Factura general", "GEN-01", 1, sale.Total, sale.Total));
            }

            var subtotal = request.Subtotal ?? (sale.SubTotal > 0 ? sale.SubTotal : sale.Total);
            var itbis = request.Itbis ?? (sale.Tax > 0 ? sale.Tax : (sale.Total * 0.18m));
            var total = request.Total ?? sale.Total;
            var discount = request.Discount ?? 0m;

            var lines = new List<string>
            {
                compName.ToUpperInvariant(),
                compCommercial,
                $"RNC: {compRnc}   Tel: {compPhone}",
                compAddress,
                new string('=', 64),
                $"FACTURA DE VENTA: {invoiceNumber}",
                $"Fecha: {sale.Date:dd/MM/yyyy HH:mm}   Caja: {cashRegister}   Cajero: {cashier}",
                $"Cliente: {custName}   (RNC/Ced: {custRnc})",
                new string('-', 64),
                string.Format("{0,-28} {1,5} {2,12} {3,12}", "DESCRIPCION", "CANT", "PRECIO", "TOTAL"),
                new string('-', 64)
            };

            foreach (var item in items)
            {
                var cleanName = item.Name.Length > 27 ? item.Name.Substring(0, 27) : item.Name;
                lines.Add(string.Format("{0,-28} {1,5:N0} {2,12:N2} {3,12:N2}", cleanName, item.Qty, item.Price, item.LineTotal));
            }

            lines.Add(new string('-', 64));
            lines.Add(string.Format("{0,48} {1,14:N2}", "Subtotal: RD$", subtotal));
            if (discount > 0)
            {
                lines.Add(string.Format("{0,48} {1,14:N2}", "Descuento: RD$", discount));
            }
            lines.Add(string.Format("{0,48} {1,14:N2}", "ITBIS (18%): RD$", itbis));
            lines.Add(new string('=', 64));
            lines.Add(string.Format("{0,48} {1,14:N2}", "TOTAL A PAGAR: RD$", total));
            lines.Add(string.Format("{0,48} {1,14}", "Metodo de Pago:", paymentMethod));
            lines.Add(new string('=', 64));
            lines.Add("           Gracias por su compra! Conserve esta factura.");
            lines.Add("        Comprobante de venta emitido electronicamente.");

            var content = new StringBuilder("BT\n/F1 10 Tf\n40 790 Td\n");
            foreach (var line in lines)
            {
                content.Append('(').Append(EscapePdfText(line)).Append(") Tj\n0 -14 Td\n");
            }
            content.Append("ET");
            var stream = content.ToString();

            var objects = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"
            };

            using var output = new MemoryStream();
            using var writer = new StreamWriter(output, Encoding.ASCII, leaveOpen: true);
            writer.WriteLine("%PDF-1.4");
            writer.Flush();
            var offsets = new List<long> { 0 };
            for (var index = 0; index < objects.Length; index++)
            {
                offsets.Add(output.Position);
                writer.WriteLine($"{index + 1} 0 obj");
                writer.WriteLine(objects[index]);
                writer.WriteLine("endobj");
                writer.Flush();
            }

            var xrefPosition = output.Position;
            writer.WriteLine($"xref\n0 {objects.Length + 1}");
            writer.WriteLine("0000000000 65535 f ");
            for (var index = 1; index < offsets.Count; index++)
                writer.WriteLine($"{offsets[index]:D10} 00000 n ");
            writer.WriteLine($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
            writer.Flush();
            return output.ToArray();
        }

        private static string EscapePdfText(string value) => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal)
            .Where(c => c >= 32 && c <= 126)
            .Aggregate(new StringBuilder(), (builder, character) => builder.Append(character))
            .ToString();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Onion.BussinesLogic.Services.Abstract;
using System.Security.Claims;

namespace Onion.Filters;

public sealed class AuditDeletionApprovalFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> EntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Product", "Category", "Customer", "Supplier", "Sale", "Purchase", "Return"
        , "Payment", "CashRegister", "CashMovement", "ProductType", "InventoryMovement"
        , "PurchaseOrderReceipt", "ManageRequest", "InvoiceSequence", "AccountReceivable"
        , "PaymentPlan", "Installment", "Credit", "CreditNote", "Appointment"
    };

    private readonly IDeletionApprovalService _approvals;
    public AuditDeletionApprovalFilter(IDeletionApprovalService approvals) => _approvals = approvals;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (!HttpMethods.IsDelete(context.HttpContext.Request.Method) || !IsRole(user, "Audit"))
        {
            await next();
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        var entityType = controller?.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) == true
            ? controller[..^"Controller".Length]
            : controller;
        if (!int.TryParse(context.RouteData.Values["id"]?.ToString(), out var entityId) ||
            string.IsNullOrWhiteSpace(entityType) || !EntityTypes.Contains(entityType))
        {
            context.Result = new BadRequestObjectResult(new { message = "This entity type does not support deletion approval." });
            return;
        }

        var reason = context.HttpContext.Request.Query["reason"].FirstOrDefault();
        var request = await _approvals.RequestAsync(entityType, entityId, reason);
        context.Result = new ObjectResult(new
        {
            message = "Deletion request submitted for Admin approval.",
            requestId = request.Id,
            status = request.Status
        }) { StatusCode = StatusCodes.Status202Accepted };
    }

    private static bool IsRole(ClaimsPrincipal user, string role) => user.Claims.Any(c =>
        (c.Type == ClaimTypes.Role || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)) &&
        string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
}

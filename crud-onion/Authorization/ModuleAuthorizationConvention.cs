using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Onion.Common.Authorization;

namespace crud_onion.Authorization;

public sealed class ModuleAuthorizationConvention : IApplicationModelConvention
{
    private static readonly IReadOnlyDictionary<string, string> Modules =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AccountReceivableController"] = "receivables", ["CreditsController"] = "receivables",
            ["AppointmentsController"] = "company", ["CategoryController"] = "sales", ["CompanyController"] = "company",
            ["CompanySettingsController"] = "company", ["CreditNoteController"] = "sales", ["CustomerController"] = "customers",
            ["DashboardController"] = "reports", ["DeletionApprovalController"] = "audit", ["InvitationsController"] = "company",
            ["InventoryController"] = "inventory", ["ManageRequestController"] = "company", ["MeController"] = "company",
            ["PaymentController"] = "sales", ["ProductController"] = "sales", ["ProductTypeController"] = "sales",
            ["PurchaseController"] = "purchases", ["PurchaseOrderReceiptController"] = "purchases", ["ProyectosController"] = "company",
            ["RoleController"] = "company", ["PermissionController"] = "company", ["ReportsController"] = "reports",
            ["ReturnController"] = "sales", ["SubscriptionController"] = "billing", ["SupplierController"] = "purchases",
            ["UserController"] = "company", ["UserManagementController"] = "company",
            ["WarehouseEntryController"] = "inventory", ["WarehouseOutletController"] = "inventory", ["WarehouseTransferController"] = "inventory",
            ["PasswordController"] = "company", ["UmProxyController"] = "company", ["SuperUsersController"] = "company", ["MetricsController"] = "reports",
            ["CashRegisterController"] = "cashregister", ["CashMovementController"] = "cashregister", ["CajaController"] = "cashregister", ["CashSessionController"] = "cashregister",
            ["SaleController"] = "sales", ["TaxpayerController"] = "company", ["FiscalSequenceController"] = "billing", ["DgiiReportsController"] = "reports"
        };

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.Attributes.OfType<AllowAnonymousAttribute>().Any() ||
                controller.Filters.OfType<AuthorizeModuleAttribute>().Any() ||
                controller.Attributes.OfType<AuthorizeModuleAttribute>().Any())
                continue;

            if (controller.ControllerType.GetCustomAttributes(typeof(AuthorizeModuleAttribute), inherit: true).Length > 0)
                continue;

            var controllerName = controller.ControllerType.Name;
            if (controllerName is "AuthController" or "ClerkWebhookController" or "WeatherForecastController" or "WarehouseController" ||
                controllerName.StartsWith("Internal", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Modules.TryGetValue(controllerName, out var module))
                throw new InvalidOperationException($"No authorization module mapping exists for {controllerName}.");

            controller.Filters.Add(new AuthorizeModuleAttribute(module));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Onion.Common.Authorization;

namespace crud_onion.Authorization;

public sealed class ModuleAuthorizationConvention : IApplicationModelConvention
{
    private static readonly IReadOnlyDictionary<string, string> Modules =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AccountReceivableController"] = "RECEIVABLES",
            ["AppointmentsController"] = "APPOINTMENTS",
            ["CategoryController"] = "SALES",
            ["CompanyController"] = "COMPANY",
            ["CompanySettingsController"] = "COMPANY",
            ["CreditsController"] = "RECEIVABLES",
            ["CreditNoteController"] = "SALES",
            ["CustomerController"] = "CUSTOMERS",
            ["DashboardController"] = "REPORTS",
            ["DeletionApprovalController"] = "AUDIT",
            ["InvitationsController"] = "COMPANY",
            ["InventoryController"] = "INVENTORY",
            ["ManageRequestController"] = "COMPANY",
            ["MeController"] = "COMPANY",
            ["PaymentController"] = "SALES",
            ["ProductController"] = "SALES",
            ["ProductTypeController"] = "SALES",
            ["PurchaseController"] = "PURCHASES",
            ["PurchaseOrderReceiptController"] = "PURCHASES",
            ["ProyectosController"] = "PROJECTS",
            ["RoleController"] = "RBAC",
            ["PermissionController"] = "RBAC",
            ["ReportsController"] = "REPORTS",
            ["ReturnController"] = "SALES",
            ["SubscriptionController"] = "BILLING",
            ["SupplierController"] = "PURCHASES",
            ["UserController"] = "USER_MANAGEMENT",
            ["UserManagementController"] = "USER_MANAGEMENT",
            ["WarehouseController"] = "INVENTORY",
            ["WarehouseEntryController"] = "INVENTORY",
            ["WarehouseOutletController"] = "INVENTORY",
            ["WarehouseTransferController"] = "INVENTORY",
            ["PasswordController"] = "USER_MANAGEMENT",
            ["UmProxyController"] = "COMPANY",
            ["SeedController"] = "COMPANY",
            ["SuperUsersController"] = "USER_MANAGEMENT",
            ["MetricsController"] = "REPORTS"
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
            if (controllerName is "AuthController" or "ClerkWebhookController" or "WeatherForecastController" ||
                controllerName.StartsWith("Internal", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Modules.TryGetValue(controllerName, out var module))
                throw new InvalidOperationException($"No authorization module mapping exists for {controllerName}.");

            controller.Filters.Add(new AuthorizeModuleAttribute(module));
        }
    }
}

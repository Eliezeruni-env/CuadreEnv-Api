using Onion.Common.Models;
using Onion.Common.Enums;

namespace Onion.Common.Services
{
    public class GlobalizationService : IGlobalizationService
    {
        // Very small localization stub: returns English messages. Expand as needed.
        public Error GetErrorInCurrentLanguage(ErrorCodes code)
        {
            return code switch
            {
                ErrorCodes.InvitationsNotFound => new Error { Code = "NOT_FOUND", Message = "Invitations not found", Language = "EN" },
                ErrorCodes.WarehousesNotFound => new Error { Code = "NOT_FOUND", Message = "Warehouses not found", Language = "EN" },
                ErrorCodes.InventoriesNotFound => new Error { Code = "NOT_FOUND", Message = "Inventories not found", Language = "EN" },
                ErrorCodes.MovementsNotFound => new Error { Code = "NOT_FOUND", Message = "Movements not found", Language = "EN" },
                ErrorCodes.UsersNotFound => new Error { Code = "NOT_FOUND", Message = "Users not found", Language = "EN" },
                ErrorCodes.SuppliersNotFound => new Error { Code = "NOT_FOUND", Message = "Suppliers not found", Language = "EN" },
                ErrorCodes.CustomersNotFound => new Error { Code = "NOT_FOUND", Message = "Customers not found", Language = "EN" },
                ErrorCodes.SalesNotFound => new Error { Code = "NOT_FOUND", Message = "Sales not found", Language = "EN" },
                ErrorCodes.PurchasesNotFound => new Error { Code = "NOT_FOUND", Message = "Purchases not found", Language = "EN" },
                ErrorCodes.CompaniesNotFound => new Error { Code = "NOT_FOUND", Message = "Companies not found", Language = "EN" },
                ErrorCodes.ReturnsNotFound => new Error { Code = "NOT_FOUND", Message = "Returns not found", Language = "EN" },
                ErrorCodes.ProductTypesNotFound => new Error { Code = "NOT_FOUND", Message = "Product types not found", Language = "EN" },
                ErrorCodes.CashRegistersNotFound => new Error { Code = "NOT_FOUND", Message = "Cash registers not found", Language = "EN" },
                ErrorCodes.CashMovementsNotFound => new Error { Code = "NOT_FOUND", Message = "Cash movements not found", Language = "EN" },
                ErrorCodes.PaymentsNotFound => new Error { Code = "NOT_FOUND", Message = "Payments not found", Language = "EN" },
                _ => new Error { Code = "INTERNAL_ERROR", Message = "An internal error occurred", Language = "EN" },
            };
        }
    }
}

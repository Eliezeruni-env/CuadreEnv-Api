Caja (POS) Module — Integration guide

Overview
This module extends Sales with POS capabilities (idempotency, cash sessions, subtotal/tax, notes) and integrates with Caja/Cash movements and Collections. The backend endpoints are in /v1/caja and /v1/AccountReceivable. Follow the FE contract below.

API endpoints (examples)

1) Create quick sale (POS)
POST /v1/caja/sales
Headers: Authorization: Bearer <token>
Body (JSON):
{
  "idempotencyKey": "client-uuid-123",
  "customerId": 10,
  "date": "2026-09-01T12:00:00Z",
  "notes": "Venta en caja",
  "cashRegisterId": 3,
  "createBy": "5",
  "items": [ { "productId": 1, "quantity": 2, "unitPrice": 250.00 }, { "productId": 2, "quantity": 1, "unitPrice": 800.00 } ]
}

Response: 201 Created with Sale object (including Id)

2) Register payment for a sale
POST /v1/sale/{id}/payments
Body:
{ "amount": 1300.00, "method": "CASH", "reference": "TKT-0001", "cashRegisterId": 3 }

Behavior: records Payment entity, updates Sale.PaidAmount and Sale.Status; when cashRegisterId provided, records CashMovement.

3) Create AccountReceivable with payment plan
POST /v1/AccountReceivable
Body sample in README_ACCOUNT_RECEIVABLE.md

Migrations
- New migration stubs added:
  - DataAccess/Migrations/20261001_AddPaymentPlansInstallments.cs
  - DataAccess/Migrations/20261002_AddCajaColumns.cs
Run locally:
  dotnet ef migrations add <name> --project DataAccess/Onion.DataAccess.csproj --startup-project crud-onion/crud-onion.csproj
  dotnet ef database update --project DataAccess/Onion.DataAccess.csproj --startup-project crud-onion/crud-onion.csproj

FE notes
- Send idempotencyKey on POST /v1/caja/sales to avoid duplicates.
- When paying and method == "CASH", include cashRegisterId to record cash movement.
- Map validation errors from payload.details for form-level errors.

Security
- Internal endpoints require X-Internal-ApiKey or proper JWT role.
- Keep cashRegisterId and other sensitive flows on server; FE never has the internal API key.

Contact
- For integration issues, include requestId from error responses when reporting to backend team.

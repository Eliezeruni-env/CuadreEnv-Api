Account Receivable / Payments API (examples)

Base: {BASE_URL}/v1

1) Create account receivable with optional payment plan
POST /v1/AccountReceivable
Headers: Authorization: Bearer <token>
Body:
{
  "companyId": 1,
  "customerId": 10,
  "saleId": null,
  "totalAmount": 25000.00,
  "paidAmount": 0.00,
  "dueDate": "2026-09-01T00:00:00Z",
  "plan": {
	"installmentAmount": 5000.00,
	"totalInstallments": 5,
	"frequency": "Monthly",
	"startsAt": "2026-09-01T00:00:00Z"
  }
}

Success: 201 { success: true, data: { id: 123, ... }, message: "Account receivable created" }

2) List account receivables (paged)
GET /v1/AccountReceivable?pageNumber=1&pageSize=20&search=juan&status=Open

3) Get AR detail and payments
GET /v1/AccountReceivable/{id}
Response includes payments: [ { id, amount, date, method, reference } ]

4) Register payment for AR
POST /v1/AccountReceivable/{id}/payments
Body:
{
  "amount": 5000.00,
  "method": "CASH",
  "reference": "PAY-2026-0001",
  "notes": "Partial payment",
  "date": "2026-09-01T15:00:00Z",
  "cashRegisterId": 3
}

Behavior:
- Registers payment and updates AR.paidAmount and status
- If cashRegisterId provided and method == CASH, records CashMovement

5) Installments
- List installments: GET /v1/AccountReceivable/{id}/installments
- Pay installment: POST /v1/AccountReceivable/{id}/installments/{installmentId}/pay
  Body: { "amount": 5000.00, "method": "CASH", "cashRegisterId": 3 }

Notes:
- Run migrations after pulling changes: 
  dotnet ef migrations add AddPaymentPlansInstallments --project DataAccess/Onion.DataAccess.csproj --startup-project crud-onion/crud-onion.csproj
  dotnet ef database update --project DataAccess/Onion.DataAccess.csproj --startup-project crud-onion/crud-onion.csproj
- Use the internal endpoint GET /internal/migrations/pending and POST /internal/migrations/apply (dev-only) to inspect/apply migrations.
- FE should show errors using the project's error payload shape and use payload.details for field-level validation errors.

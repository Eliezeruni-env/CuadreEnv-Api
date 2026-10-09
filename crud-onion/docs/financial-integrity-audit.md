# CuadreEnv financial integrity audit

## Decisions and implementation status

1. **Cash concurrency**
   - **Diagnosis:** `CashRegister.RowVersion` is configured as a SQL Server `rowversion`; close uses `If-Match`/ETag and translates a lost update into a conflict. The database trigger `TR_CashRegisters_ImmutableAfterClose` prevents updates after closure.
   - **Decision:** Fail explicitly on a stale ETag or a second close. Never merge two closure results.
   - **Changes:** `CashRegister.Close` is the write-once domain operation; `CashRegisterService` catches `DbUpdateConcurrencyException`.
   - **Verification:** Domain test covers the second-close rule. SQL migration must be applied in each environment.

2. **Inventory concurrency**
   - **Diagnosis:** Stock reservation and commit use conditional SQL `UPDATE` statements, so two sales cannot both consume the last available quantity.
   - **Decision:** Reject the losing sale with `INSUFFICIENT_STOCK`; do not silently retry a cashier operation.
   - **Changes:** Existing `TryReserveStockAsync`/`TryCommitReservedStockAsync` remain the atomic conflict boundary.
   - **Verification:** Requires a concurrent SQL integration test against the target SQL Server instance; no integration harness exists yet.

3. **Sale, stock, cash, and fiscal atomicity**
   - **Diagnosis:** The POS `CajaService` previously saved a sale independently and bypassed stock/cash/fiscal processing.
   - **Decision:** One database transaction covers sale, details, stock movements, cash movement, fiscal outbox row, and invoice sequence. DGII/printer delivery is asynchronous and must not be treated as part of a local DB transaction.
   - **Changes:** `CajaService` delegates to `SaleService.CreateAsync`.
   - **Verification:** Source path uses `BeginTransactionAsync`, commit, and rollback. External fiscal delivery is represented by the outbox state machine.

4. **Blind cash count**
   - **Diagnosis:** Expected amount is calculated server-side only when the close command is submitted. The close response is `{ closed: true }` and does not return expected/difference.
   - **Decision:** The backend, not the frontend, controls the blind-close rule. The client submits only the physical count and optional breakdown.
   - **Changes:** `CloseCashRegisterRequest` contains `ActualAmount` and `BreakdownJson`; expected amount is not accepted from the client.
   - **Verification:** Review/API test must assert that the close response does not contain expected amount. A post-close read may expose the finalized audit record to authorized users.

5. **Discrepancy calculation**
   - **Diagnosis:** Calculation is isolated in `CashDiscrepancy` and rounds expected, actual, and difference to the currency scale using banker's rounding.
   - **Decision:** Positive is surplus, negative is shortage, zero is exact.
   - **Changes:** Added `IsSurplus`, `IsShortage`, and `IsExact` domain classifications.
   - **Verification:** Unit tests cover all three cases and rounding.

6. **Immutable closure history**
   - **Diagnosis:** Domain state is write-once and SQL trigger enforcement rejects updates to closed registers. Cash movements are append-only at the database level.
   - **Decision:** Corrections require a new adjustment/audit event, never editing the old closure.
   - **Changes:** `IsImmutable`, private closure amount, domain guard, and SQL triggers are in place.
   - **Verification:** Migration `HardenFinancialIntegrity` must be applied; direct SQL update should be tested in deployment validation.

7. **Granular authorization**
   - **Diagnosis:** Module authorization is centralized through `ModuleAuthorizationConvention`, which applies `AuthorizeModuleAttribute` to unmapped controllers and fails startup for a missing mapping. Permission checks currently remain explicit attributes because there is no authoritative controller/action-to-permission map.
   - **Decision:** Do not invent permission names. Any new controller must receive a module mapping; permission policy additions must be explicit and reviewed.
   - **Verification:** Startup convention behavior is source-verified; endpoint authorization matrix tests remain a gap.

8. **USM startup license**
   - **Diagnosis:** The current policy is fail-closed; missing/unresponsive USM blocks startup.
   - **Decision:** Keep fail-closed for financial integrity. Retry a bounded number of times, then stop startup. No unsigned local grace cache is trusted.
   - **Changes:** Added `Usm:StartupRetryCount` and bounded retry delay. Timeout remains explicit through the named HttpClient.
   - **Verification:** Configuration and source behavior are verified; an unavailable-USM deployment test is still required.

9. **DGII fiscal delivery**
   - **Diagnosis:** Fiscal documents already have durable retry state and a separate submission audit table.
   - **Decision:** Use the deterministic `SALE:{saleId}` document key as the idempotency key. Retry transient failures only; permanently reject non-retryable 4xx responses.
   - **Changes:** `FiscalDocument.ForSale`, unique document key index, DGII `Idempotency-Key` header, retry scheduling, and legal audit events.
   - **Verification:** Unit test covers canonical key; DGII contract test must confirm the provider honors the idempotency key.

10. **Errors and resilience**
	- **Diagnosis:** USM and DGII clients have explicit timeouts. Fiscal retry is persisted, not in-memory.
	- **Decision:** Bounded startup retry for USM and durable exponential scheduling for DGII; no unbounded request retry.
	- **Verification:** Configuration is source-verified; timeout and retry behavior need integration tests with fault injection.

11. **Tests**
	- **Implemented:** 5 domain tests cover discrepancy rounding/classification, immutable closure, and fiscal key generation.
	- **Required next:** SQL Server integration tests for concurrent last-unit sales, stale ETag close, complete sale transaction rollback, and opening→sale→close. Add a load-test project (k6 or NBomber) for concurrent POS sessions.
	- **Verification blocker:** The existing `tests/Onion.BusinessLogic.Tests` project had incorrect project-reference paths; those were corrected, but its tests still target an older `SaleService` constructor/repository contract and are not currently executable. The integration project is outside `crud-onion.slnx` and depends on LocalDB/WebApplicationFactory configuration.
	- **Load scaffold:** `tests/load/pos-concurrency.js` provides a k6 scenario. Run it against a deployed test database with `BASE_URL`, `JWT`, `CASH_REGISTER_ID`, `PRODUCT_ID`, `VUS`, and `DURATION`; it treats successful sales, explicit stock conflicts, and validation conflicts as expected outcomes, while failing on 5xx responses.

12. **API and flow documentation**
	- **Implemented:** Swagger is configured in `Program.cs`; this document records the financial contract and flow.

## Critical flow

```mermaid
sequenceDiagram
	participant POS
	participant API
	participant DB
	participant DGII

	POS->>API: POST /v1/caja/sales + Idempotency-Key
	API->>DB: Begin transaction
	API->>DB: Insert sale/details
	API->>DB: Conditional reserve/commit stock
	API->>DB: Insert inventory movement
	API->>DB: Insert cash movement
	API->>DB: Allocate invoice sequence
	API->>DB: Insert Pending FiscalDocument (unique SALE:{saleId})
	API->>DB: Commit transaction
	API-->>POS: Sale accepted; fiscal delivery pending
	DGII->>API: Outbox worker submits Idempotency-Key
	API->>DB: Append FiscalSubmissionAudit
	API->>DB: Mark Accepted, Retry, or Rejected
```

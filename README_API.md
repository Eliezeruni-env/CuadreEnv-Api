API quick reference — example requests/responses

Base URL
- Replace with your deployment: https://api.yourdomain.com/

Authentication
- Use Bearer token in Authorization header: Authorization: Bearer <jwt>
- Token must include claim "companyId" so the backend can assign tenant scope automatically.

Common headers
- Content-Type: application/json
- Accept: application/json

Products

1) Create product
POST /api/products
Request headers:
- Authorization: Bearer <token>

Request body (JSON):
{
  "description": "Example product",
  "barcode": "1234567890123",
  "productTypeId": 1,
  "categoryId": 2,
  "companyId": 1,
  "cost": 10.5,
  "minimumQuantity": 0
}

Successful response (201 or 200 depending on controller):
{
  "id": 123,
  "description": "Example product",
  "barcode": "1234567890123",
  "productTypeId": 1,
  "categoryId": 2,
  "companyId": 1,
  "cost": 10.5,
  "minimumQuantity": 0,
  "creationDate": "2026-01-01T12:00:00Z"
}

Common error response (validation or business rule):
HTTP 400
{
  "statusCode": 400,
  "message": "Product name already exists",
  "errorCode": "DUPLICATE_NAME",
  "requestId": "00-...",
  "path": "/api/products",
  "method": "POST",
  "timestamp": "2026-01-01T12:00:00Z"
}

2) Get list of products
GET /api/products
Query params: pageNumber, pageSize
Response:
{
  "items": [ { /* product DTO */ } ],
  "pageSize": 20,
  "pageCount": 3,
  "total": 55
}

3) Get product by id
GET /api/products/{id}
Response: product object or 404 with error payload

Errors / diagnostics (how FE can help debugging)
- The API returns a requestId (TraceIdentifier) with error responses. If user reports an issue, ask for the requestId and timestamp so backend logs can be correlated.
- In Development environment, the API includes exception details (message, stackTrace, inner) in the JSON. In Production, only safe fields are returned.

cURL example (create):
curl -X POST "https://api.yourdomain.com/api/products" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"description":"Example product","productTypeId":1,"categoryId":2,"companyId":1,"cost":10.5,"minimumQuantity":0}'

Notes for frontend integration
- Always include the Authorization header. Backend uses the token's companyId claim to set tenant automatically (you may still send companyId in body for explicitness but token claim is authoritative).
- Show friendly messages using the "message" returned by the API, and optionally surface errorCode for special handling (e.g., DUPLICATE_NAME -> show specific UI hint).
- When an error occurs, capture requestId and timestamp and include them in bug reports to the backend team.

Contact
- For integration issues, provide: endpoint, HTTP method, requestId (response), timestamp, and request payload (no secrets).

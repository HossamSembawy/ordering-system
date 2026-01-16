# Order Service

## Service Responsibility

The **Order Service** orchestrates the end-to-end order lifecycle:
- Accept and validate customer order structure
- Check stock against **seeded inventory** (local to Order Service)
- Deduct stock when order is valid (reserves inventory)
- Persist only **valid orders with deducted stock** to the database
- Create fulfillment tasks in the **Fulfillment Service**
- Manage order status transitions
- Listen for fulfillment task updates and sync order status
- Integrate with cron job for status reconciliation

**Key:** Order Service owns and manages inventory via seeding; no API calls to Fulfillment Service for inventory

---

## Architecture & Integration

```
Customer Request → Order Service (Orchestration)
  ├─ (Idempotency) Check Idempotency-Key for existing order
  ├─ Validate Request (structure, qty > 0)
  ├─ Check & Deduct Stock (seeded inventory in Order Service)
  │   └─ If fails: 404/409, order NOT created
  ├─ Persist Order (Order Service DB)
  ├─ Create Fulfillment Task (call Fulfillment Service)
  │   └─ POST /tasks {orderId}
  └─ Return 201 orderId + status

Cron Job (every 5 min)
  └─ Reconcile status by polling Fulfillment Service
```

**Key Principles:**
- **Inventory is seeded and managed locally** in Order Service (no API calls for stock)
- Stock deduction happens during order creation (before persistence)
- **Only orders with successfully deducted stock** are persisted to database
- Fulfillment Service is called ONLY for task creation and status updates
- Status updates come via webhooks/callbacks (no polling)
- **Idempotency** is enforced on POST /orders via `Idempotency-Key`
- No shared databases between services; no mocks in production

---

## API Endpoints

### 1. POST /orders — Create Order

**Request:**
```http
POST /orders
Authorization: Bearer <token>
Idempotency-Key: <unique-key-per-user-request>
Content-Type: application/json

{
  "items": [
    { "productId": "PRODUCT_1", "qty": 2 },
    { "productId": "PRODUCT_2", "qty": 5 }
  ]
}
```

**Headers:**
- `Authorization` (required): User ID is extracted from the bearer token or custom header (e.g., `X-User-Id`)
- `Idempotency-Key` (required for deduplication): Unique per user per logical order request. Reusing the same key returns the original order without re-consuming inventory.

**Request Body:**
- `items[]` (required): Array of order items
  - `productId` (required): Product identifier
  - `qty` (required): Quantity (must be > 0)

**Processing (idempotent):**
1. **Idempotency check**: Look up `(userId, Idempotency-Key)`. If found, return the existing order response (no new stock deduction or task creation).
2. Validate request structure
3. **Check & deduct stock** from seeded inventory (local to Order Service)
4. If stock deduction succeeds: persist order to database
5. Create fulfillment task in Fulfillment Service (async, fire-and-forget)
6. Return 201

**Response (201 Created) — Success:**
```json
{
  "orderId": "ORD-20260115-001",
  "status": "pending",
  "createdAt": "2026-01-15T10:30:00Z"
}
```

**Idempotent Replays:**
- If the same `Idempotency-Key` is sent again by the same user, return the original order payload (200 or 201-equivalent) without re-consuming inventory or creating another task.

**Error Responses:**

| Status | Error Code | Condition |
|--------|-----------|-----------|
| 400 | `INVALID_REQUEST` | Missing/empty `items`, qty ≤ 0, malformed request |
| 401 | `UNAUTHORIZED` | Missing or invalid user ID in header |
| 404 | `PRODUCT_NOT_FOUND` | Unknown `productId` in seeded inventory |
| 409 | `INSUFFICIENT_STOCK` | Requested qty exceeds available stock; **order NOT created** |
| 500 | `INTERNAL_ERROR` | Server error (stock deduction) |

**Error Response Format:**
```json
{
  "error": "INSUFFICIENT_STOCK",
  "message": "Not enough stock for PRODUCT_1 (requested: 10, available: 5)"
}
```

**Note:** If stock deduction fails (404 product not found or 409 insufficient stock), no order is created in database. Inventory is managed locally via seeding.

---

### 2. GET /orders/{orderId} — Retrieve Order

**Request:**
```http
GET /orders/{orderId}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "orderId": "ORD-20260115-001",
  "userId": "user123",
  "items": [
    { "productId": "PRODUCT_1", "qty": 2 },
    { "productId": "PRODUCT_2", "qty": 5 }
  ],
  "status": "pending",
  "createdAt": "2026-01-15T10:30:00Z",
  "updatedAt": "2026-01-15T10:35:00Z"
}
```

**Error Responses:**

| Status | Error Code | Condition |
|--------|-----------|-----------|
| 404 | `ORDER_NOT_FOUND` | Order does not exist |
| 401 | `UNAUTHORIZED` | Invalid authorization |
| 500 | `INTERNAL_ERROR` | Server error |

---

### 3. PATCH /orders/{orderId}/status — Update Order Status

**Request:**
```http
PATCH /orders/{orderId}/status
Authorization: Bearer <token>
Content-Type: application/json

{
  "status": "completed"
}
```

**Valid Status Transitions:**
- `pending` → `completed`
- `pending` → `failed` (delete)

**Response (200 OK):**
```json
{
  "orderId": "ORD-20260115-001",
  "status": "completed",
  "updatedAt": "2026-01-15T10:40:00Z"
}
```

**Error Responses:**

| Status | Error Code | Condition |
|--------|-----------|-----------|
| 400 | `INVALID_STATUS_TRANSITION` | Invalid status or transition not allowed |
| 404 | `ORDER_NOT_FOUND` | Order does not exist |
| 401 | `UNAUTHORIZED` | Invalid authorization |
| 500 | `INTERNAL_ERROR` | Server error |

---

## Order Status Lifecycle

**Simplified States:**

- **pending**: Order accepted, validated, inventory deducted, and persisted; fulfillment is outstanding (task may be not-yet-created or executing).
- **completed**: Fulfillment finished successfully; order delivered.
- **failed**: Operation did not complete successfully. Policy: failed orders are not stored — they are deleted.

**State Transition Diagram:**
```
pending ──→ completed
  │
  └────→ failed (delete)
```

---

## Processing Flow

### Happy Path (Order Accepted — Async)

1. **Idempotency Check**
   - If `(userId, Idempotency-Key)` exists: return existing order response; skip stock deduction and persistence.
2. **Validate Request**
   - User ID from header
   - Items array not empty
   - All items have productId and qty > 0
3. **Check & Deduct Stock** (seeded inventory in Order Service)
   - Check if all products exist in seeded inventory
   - Check if all quantities are available
   - Deduct quantities from inventory (atomic operation)
   - If any check fails: return 404 (product missing) or 409 (insufficient stock), order NOT created
4. **Persist Order** (ONLY if stock successfully deducted)
   - Insert into `orders` table with status = `pending`
   - Insert into `order_items` table
   - Record `(userId, Idempotency-Key) → orderId` to ensure replay returns the same order
5. **Return Response** (201 Created) — IMMEDIATE
   - Return orderId, status = `pending`, and timestamp
   - **Client receives response here**
6. **Create Fulfillment Task** (ASYNC — fire and forget)
   - Spawn background task to call `POST /tasks` with orderId
   - Do NOT wait for response
   - Do NOT block client
   - If task creation fails: order remains in `pending` state (cron job will retry). If Fulfillment ultimately rejects the operation, the order is deleted (not stored as failed).

**Critical:**
- Stock deduction (step 3) happens BEFORE order persistence (step 4)
- Order returned to client in step 5 BEFORE task creation (step 6) completes
- If task creation fails, order still exists; cron job handles reconciliation
- Client doesn't wait for task creation
- Idempotency ensures no double charging of inventory or duplicate orders for the same request

### Status Updates from Fulfillment Service

When fulfillment progresses:
- **Webhook/Callback** from Fulfillment Service when task status changes:
  - `CREATED`/`ASSIGNED`/`IN_PROGRESS` → Order remains `pending`
  - `COMPLETED` → Order status = `completed`
  - `REJECTED`/terminal failure → Order is deleted (not stored as failed)

- **Cron Job** (reconciliation, runs every 5 minutes):
  - Poll Fulfillment Service for task status of orders in `pending`
  - Sync order status if discrepancies detected (e.g., task still not created after 5 mins)
  - No real-time polling; cron ensures eventual consistency

---

## Error Handling & Response Codes

| HTTP Status | Use Case | Synchronous? |
|-------------|----------|--------------|
| 201 | Order created successfully (task creation async) | Yes |
| 200 | Successful GET, PATCH, or idempotent replay of POST | Yes |
| 400 | Invalid request structure or invalid status transition | Yes |
| 401 | Missing/invalid authorization header | Yes |
| 404 | Not found (order or product) | Yes |
| 409 | Business conflict (insufficient stock) | Yes |
| 503 | Service unavailable | Yes |

**Note:** All responses are synchronous. Task creation failure does NOT affect response to client; order remains `pending` until task status updates come via webhook/cron.

---

## Test Cases (Happy Path & Failure Cases)

### Happy Path Tests

1. **Create order with valid items → 201**
   - Given: valid userId, 2+ products with sufficient stock
   - When: POST /orders with `Idempotency-Key`
   - Then: returns 201, orderId, status=pending, order persisted; idempotency record stored

2. **Create order → Fulfillment task created (async) → order stays pending**
   - Given: valid order
   - When: POST /orders succeeds
   - Then: Fulfillment Service task is created asynchronously; order remains `pending` until completion

3. **Retrieve valid order → 200**
   - Given: orderId exists
   - When: GET /orders/{orderId}
   - Then: returns 200 with order details

4. **Update order status (valid transition) → 200**
   - Given: order is `pending`
   - When: PATCH /orders/{orderId}/status with status=completed
   - Then: returns 200, status updated to completed

### Validation Failure Tests (Should Fail with Correct Error)

5. **POST /orders returns 400 when items missing**
   - Given: request has no items field
   - When: POST /orders
   - Then: returns 400 `INVALID_REQUEST`

6. **POST /orders returns 400 when items empty**
   - Given: request has items = []
   - When: POST /orders
   - Then: returns 400 `INVALID_REQUEST`

7. **POST /orders returns 400 when qty ≤ 0**
   - Given: any item has qty = 0 or negative
   - When: POST /orders
   - Then: returns 400 `INVALID_REQUEST`

8. **POST /orders returns 401 when user ID missing**
   - Given: Authorization header missing or invalid
   - When: POST /orders
   - Then: returns 401 `UNAUTHORIZED`

### Business Logic Failure Tests

9. **POST /orders returns 404 when product not found**
   - Given: productId does not exist in seeded inventory
   - When: POST /orders
   - Then: returns 404 `PRODUCT_NOT_FOUND`, order NOT created

10. **POST /orders returns 409 when stock insufficient**
    - Given: requested qty > available stock in seeded inventory
    - When: POST /orders
   - Then: returns 409 `INSUFFICIENT_STOCK`, order NOT created, inventory unchanged

11. **POST /orders remains 201 when Fulfillment Service unavailable (async)**
  - Given: Fulfillment Service is down
  - When: POST /orders (order persisted; async task creation fails initially)
  - Then: returns 201; order stays `pending`; cron/webhook will retry task creation

### Status Transition Tests

12. **PATCH returns 400 on invalid status transition**
  - Given: order status = completed
  - When: PATCH /orders/{orderId}/status with status=pending
  - Then: returns 400 `INVALID_STATUS_TRANSITION`

13. **PATCH returns 404 when order not found**
    - Given: orderId does not exist
    - When: PATCH /orders/{orderId}/status
    - Then: returns 404 `ORDER_NOT_FOUND`

14. **GET returns 404 when order not found**
    - Given: orderId does not exist
    - When: GET /orders/{orderId}
    - Then: returns 404 `ORDER_NOT_FOUND`

### Idempotency & Concurrency Tests

15. **Duplicate order requests (same user, same Idempotency-Key) return the same order**
    - Given: two identical order requests with the same `Idempotency-Key`
    - When: POST /orders twice
    - Then: only one order persisted; second call returns the original order (no extra stock deduction or new task)

16. **Concurrent requests don't create duplicate orders**
    - Given: same userId, same items, same `Idempotency-Key`, concurrent requests
    - When: POST /orders × 2 simultaneously
    - Then: only one order in database; consistent response with the same orderId

---

## Inventory Seeding Approach

**Overview:** Order Service manages inventory via seeding (not API calls). Inventory is initialized at startup and maintained locally.

**Seeding Options:**
- In-memory for development/testing
- Database-backed for persistence across restarts

**Behavior:**
- Stock is deducted atomically during order creation
- On failure (product missing or insufficient stock), no inventory changes occur
- Inventory changes only via successful order creation (idempotent: no double-deduction)

### Cron Job
- Enabled by default; runs every 5 minutes
- Reconciles order status with Fulfillment Service
- Handles missed updates gracefully

## 1) Service responsibility

The **Fulfillment Service** is responsible for operational fulfillment execution via a **single human-worker task per order**.

**Responsibilities**
- Create exactly **one task per order** (**idempotent** by `orderId`).
- Orchestrate tasks to human workers.
- Enforce: **each worker can have at most 5 active tasks** simultaneously.
  - Active = `ASSIGNED` + `IN_PROGRESS`
- Allow workers to update task status.
- Notify the **Order Service** when task status changes so the Order state is updated.

**High-level flow (chart)**


## 2) API endpoints

### 2.1 Create task (called by Order Service)

**POST** `/tasks`

**Request**
```json
{ "orderId": "ORD-123" }
```

**Behavior**
- If no task exists for `orderId`: create a new task with status `CREATED`.
- If a task already exists for `orderId`: return the existing task (**idempotent**).

**Responses**
- `201 Created` (new task)
- `200 OK` (task already existed)

**Response**
```json
{
  "taskId": "c2d9a2a1-0b5c-44d8-9c46-7c3f5a9f1f21",
  "orderId": "ORD-123",
  "status": "CREATED",
  "workerId": null
}
```

---

### 2.2 Claim a task (human worker pulls work)

**POST** `/workers/{workerId}/tasks/claim`

**Behavior**
- Enforces **max 5 active tasks** for the worker (`ASSIGNED` + `IN_PROGRESS`).
- If allowed, assigns the oldest available task (`CREATED`) to the worker.

**Responses**
- `200 OK` — task assigned
- `204 No Content` — no available tasks
- `409 Conflict` — worker already has 5 active tasks

**Response (200)**
```json
{
  "taskId": "c2d9a2a1-0b5c-44d8-9c46-7c3f5a9f1f21",
  "orderId": "ORD-123",
  "status": "ASSIGNED",
  "workerId": "W-7"
}
```

**Error (409)**
```json
{ "code": "WORKER_CAPACITY_EXCEEDED", "message": "Worker already has 5 active tasks." }
```

---

### 2.3 Update task status (worker processing)

**PATCH** `/tasks/{taskId}`

**Request examples**
```json
{ "status": "IN_PROGRESS" }
```

```json
{ "status": "COMPLETED" }
```

```json
{ "status": "REJECTED" }
```

**Valid transitions**
- `ASSIGNED -> IN_PROGRESS`
- `IN_PROGRESS -> COMPLETED | REJECTED`

**Responses**
- `200 OK`
- `400 Bad Request` — invalid transition
- `404 Not Found`

---

### 2.4 Update order status

**PATCH** `/orders/{orderId}`

**Request examples**
```json
{ "status": "IN_PROGRESS" }
```

```json
{ "status": "COMPLETED" }
```

```json
{ "status": "REJECTED" }
```

**Valid transitions**
- `ASSIGNED -> IN_PROGRESS`
- `IN_PROGRESS -> COMPLETED | REJECTED`

**Responses**
- `200 OK`
- `400 Bad Request` — invalid transition
- `404 Not Found`

---

### 2.5 Read endpoints (useful for debugging/tests)

- **GET** `/tasks/{taskId}`
- **GET** `/tasks/by-order/{orderId}`
- **GET** `/workers/{workerId}/tasks?activeOnly=true`

---

## 3) Order states

- **pending:** Order recieved by the service, validation in progress, fulfillment task not yet created
- **assigned:** Fulfillment task created in fulfillment service, task in queue or assigned to worker, waiting for worker to start on order
- **in_progress:** Worker has started on the task
- **completed:** Worker finished the task, order has been fulfilled
- **rejected:** order could not be accepted or completed (bad request/ insufficient stock), no fulfillment

## 4) Initial TDD tests (failing first)

### Test suite A — Task creation
1. **Create task returns `201` and creates a new task**
   - Given: `orderId` is new
   - When: `POST /tasks` with `orderId`
   - Then: response contains `taskId`, `status=CREATED`, and the task is persisted

2. **Create task is idempotent by `orderId`**
   - Given: a task already exists for `orderId`
   - When: `POST /tasks` again with the same `orderId`
   - Then: returns `200` and the **same** `taskId`

3. **Exactly one task per order is enforced**
   - Given: concurrent requests to create task for the same `orderId`
   - When: two `POST /tasks` happen at the same time
   - Then: only one task row exists; both callers observe the same `taskId`

### Test suite B — Claim & worker capacity (max 5 active)
4. **Worker can claim a task when under capacity**
   - Given: worker has 0–4 active tasks (`ASSIGNED`/`IN_PROGRESS`) and at least one `CREATED` task exists
   - When: `POST /workers/{workerId}/tasks/claim`
   - Then: returns `200`, task becomes `ASSIGNED`, `workerId` is set

5. **Worker cannot claim when already has 5 active tasks**
   - Given: worker has 5 active tasks
   - When: claim
   - Then: returns `409 WORKER_CAPACITY_EXCEEDED`

6. **Claim returns `204` when no tasks are available**
   - Given: there are no `CREATED` tasks
   - When: claim
   - Then: returns `204 No Content`

7. **Claim picks the oldest available task**
   - Given: multiple tasks in `CREATED` state with different creation times
   - When: claim
   - Then: the earliest created task is assigned

8. **Two workers cannot claim the same task**
   - Given: one `CREATED` task
   - When: two different workers claim concurrently
   - Then: exactly one receives the task; the other receives `204` (or a different task if available)

### Test suite C — Status transitions
9. **Invalid transition is rejected**
   - Example: `CREATED -> COMPLETED` should return `400`

10. **Valid transitions succeed**
   - `ASSIGNED -> IN_PROGRESS` returns `200`
   - `IN_PROGRESS -> COMPLETED` returns `200`
   - `IN_PROGRESS -> REJECTED` returns `200`

### Test suite D — Order Service notification (mocked)
11. **Notifies Order Service when task becomes IN_PROGRESS**
   - Given: task transitions `ASSIGNED -> IN_PROGRESS`
   - Then: Order Service client is called with `FULFILLMENT_IN_PROGRESS`

12. **Notifies Order Service when task becomes COMPLETED**
   - Given: task transitions `IN_PROGRESS -> COMPLETED`
   - Then: Order Service client is called with `FULFILLED`

13. **Notifies Order Service when task becomes FAILED**
   - Given: task transitions `IN_PROGRESS -> REJECTED`
   - Then: Order Service client is called with `FULFILLMENT_FAILED`

# Service Responsibility

- Accepting Orders
- Validating Orders by checking request body and business checks (stock, duplicate orders, maximum qty)
- Checking stock and removing stock when an order is accepted
- Storing Orders and Order Items in a Database
- Creates a fulfillment task
- Manages Order status

---

## API Endpoints

### POST /orders

**Request:**
```json
{
    "userId" : "user123",
    "items" :[
        {
            "productId" : "PRODUCT_1", "qty" : 2
        },
        {
            "productId" : "PRODUCT_2", "qty" : 2
        }
    ]
}
```

**Response:**
```json
{
    "orderId" : "1",
    "status"  : "assigned",
    "taskId"  : "1"
}
```

**Fail Response:**

**400 Bad Request:** Invalid Request Structure (missing fields, empty items, qty <= 0)
```json
{
  "error": "INVALID_REQUEST",
  "message": "Items must have a value and qty must be greater than zero"
}
```

**422 Unprocessable Content:** business validation error (unknown productId, insufficient stock)

```json
{
  "error": "PRODUCTID_NOT_FOUND",
  "message": "The requested product does not exist"
}
```

```json
{
  "error": "INSUFFICIENT_STOCK",
  "message": "Not enough stock available for the requested quantity"
}
```

**503 Service Unavailable:** Fulfillment service fail
```json
{
  "error": "FULFILLMENT_UNAVAILABLE",
  "message": "Unable to create fulfillment task at this time. Please try again later."
}
```

---

### GET /orders/{orderId}

**Response:**
```json
{
  "orderId": "1",
  "userId": "user123",
  "items": [
    { "productId": "PRODUCT_1", "qty": 2 },
    { "productId" : "PRODUCT_2", "qty" : 2 }
  ],
  "status": "in_progress",
  "taskId": "1"
}
```

**Fail Response:**

**404 Not found:**
```json
{
    "error" : "ORDER_NOT_FOUND"
}
```

---

## Order States

- **pending:** Order recieved by the service, validation in progress, fulfillment task not yet created
- **assigned:** Fulfillment task created in fulfillment service, task in queue or assigned to worker, waiting for worker to start on order
- **in_progress:** Worker has started on the task
- **completed:** Worker finished the task, order has been fulfilled
- **rejected:** order could not be accepted or completed (bad request/ insufficient stock), no fulfillment

### State Transition

- pending -> assigned -> in_progress -> completed
- any state -> rejected (on failure)

---

## Failing Tests

**Validation + inventory checks**
- POST /orders returns 400 when items is missing
- POST /orders returns 400 when items is empty
- POST /orders returns 400 when any item has qty <= 0
- POST /orders returns 422 when productId does not exist in inventory
- POST /orders returns 422 when requested qty is more than available stock
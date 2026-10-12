# CLDV6212_part2

## Member 2 — Queue Consumer & Orders Table (ST10472017)

Everything after the message lands on `order-processing-queue`.

`ProcessOrderQueue` fires on each queue message, parses the JSON payload and writes
a row to the `Orders` table (`PartitionKey` = order date, `RowKey` = OrderId,
`Status` = `Received`). Writes are idempotent: queues deliver at least once, so a
redelivered message finds the existing row and is treated as success rather than
failing on a duplicate key.

The row then moves through `Received → Preparing → Ready → Collected`, one step every
`STATUS_DELAY_SECONDS`. Status updates are ETag-checked, so a manual change through the
API stops the simulation instead of overwriting it.

`host.json` sets `maxDequeueCount` to 3. A message that keeps failing is retried three
times and then moved to `order-processing-queue-poison`, where `HandlePoisonOrder` logs
it and records the order with status `Failed`.

### Endpoints

| Method | Route | Success | Failure |
| --- | --- | --- | --- |
| GET | `/api/orders` | 200 | 400 on an unknown `?status=` filter |
| GET | `/api/orders/{orderId}` | 200 | 404 |
| PUT | `/api/orders/status` | 200 | 400, 404, 409 |

A status may only move to the step directly after the current one; anything else
returns 409.

### Settings

| Setting | Meaning |
| --- | --- |
| `STATUS_DELAY_SECONDS` | Seconds between simulated barista steps. `0` turns the simulation off. |
| `ENABLE_POISON_TEST` | When `true`, an order whose `CustomerName` is `POISON-TEST` fails deliberately, to demonstrate the poison queue. |

### Tests

`docs/wip/member2_orders_processing.postman_collection.json` holds 15 requests carrying
28 assertions across the status lifecycle, the manual transition rules, the error codes
and the poison queue.

```bash
newman run docs/wip/member2_orders_processing.postman_collection.json \
  -e docs/wip/member2_orders.postman_environment.json
```

The run takes about 50 seconds: two requests wait for the simulated lifecycle and for
the three queue retries.
